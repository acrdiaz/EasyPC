using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace EasyPC
{
    class Program
    {
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            // Load configuration from appsettings.json
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Read commands and delays from configuration
            var commands = config.GetSection("Commands").Get<string[]>();
            int delayBetweenCommandsMs = config.GetValue<int>("DelayBetweenCommandsMs");
            int loopDelayMs = config.GetValue<int>("DelayBetweenLoopsMs");

            Console.WriteLine("Welcome to the EasyPC!");
            Console.WriteLine("The app will start running commands automatically in the background.");
            Console.WriteLine("Press Ctrl+C to stop the application...");

            if (string.IsNullOrEmpty(commands?.ToString()))
            {
                Console.WriteLine("\nThere is nothing to run: appsettings.json file is empty");
                Console.WriteLine("\nStopping the application...");
                Environment.Exit(0);
            }

            // Start the background task
            Task commandTask = RunCommandsAsync(commands, delayBetweenCommandsMs, loopDelayMs, _cancellationTokenSource.Token);


            // Handle graceful shutdown when the user presses Ctrl+C
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Console.WriteLine("\nStopping the application...");
                _cancellationTokenSource.Cancel(); // Signal the task to stop
                eventArgs.Cancel = true; // Prevent the process from terminating immediately

                Environment.Exit(0);
            };

            try
            {
                await commandTask; // Wait for the task to complete
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Application stopped.");
            }
        }

        static async Task RunCommandsAsync(
            string[] commands, 
            int delayBetweenCommandsMs, 
            int loopDelayMs, 
            CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                foreach (var command in commands)
                {
                    try
                    {
                        await RunCommandAsync(command, cancellationToken);
                        await Task.Delay(delayBetweenCommandsMs, cancellationToken); // Delay between commands
                    }
                    catch (OperationCanceledException)
                    {
                        break; // Exit the loop if cancellation is requested
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred: {ex.Message}");
                    }
                }

                await Task.Delay(loopDelayMs, cancellationToken); // Delay before repeating the loop
            }
        }

        static async Task RunCommandAsync(string command, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Executing command: {command}");

            var processInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process())
            {
                process.StartInfo = processInfo;
                process.Start();

                // Read the output asynchronously
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();

                await process.WaitForExitAsync(cancellationToken);

                if (!string.IsNullOrEmpty(output))
                {
                    Console.WriteLine("Output:");
                    Console.WriteLine(output);
                }

                if (!string.IsNullOrEmpty(error))
                {
                    Console.WriteLine("Error:");
                    Console.WriteLine(error);
                }
            }
        }
    }
}
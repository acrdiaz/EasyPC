using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace EasyPC
{
    class Program
    {
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            Console.WriteLine("Welcome to the EasyPC!");
            Console.WriteLine("The app will start running commands automatically in the background.");
            Console.WriteLine("Press Ctrl+C to stop the application...");

            // Start the background task
            Task commandTask = RunCommandsAsync(_cancellationTokenSource.Token);

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

        static async Task RunCommandsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await RunCommandAsync("net stop w3svc", cancellationToken);

                    await Task.Delay(5000, cancellationToken);

                    await RunCommandAsync("taskkill -f -im explorer.exe", cancellationToken);

                    await Task.Delay(10000, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // This exception is expected when the task is canceled
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
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
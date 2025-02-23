using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Extensions.Configuration;

namespace EasyPC
{
    class Program
    {
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        static async Task Main(string[] args)
        {
            OSWindowsValidate();

            (string[] commands, int delayBetweenCommandsMs, int loopDelayMs) = LoadCommandsToMemory();

            await WorkerSetup(commands, delayBetweenCommandsMs, loopDelayMs);
        }

        private static async Task WorkerSetup(string[] commands, int delayBetweenCommandsMs, int loopDelayMs)
        {
            // Start the background task
            Task commandTask = RunCommandsAsync(commands, delayBetweenCommandsMs, loopDelayMs, _cancellationTokenSource.Token);

            // Handle user input for additional commands
            //Task userInputTask = HandleUserInputAsync(_cancellationTokenSource.Token);

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
                await Task.WhenAll(
                    commandTask
                    //, userInputTask
                    ); // Wait for tasks to complete
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Application stopped.");
            }
        }

        private static void OSWindowsValidate()
        {
            // Check if the application is running on Windows
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("This application is designed to run only on Windows.");
                Console.WriteLine("Exiting...");
                return; // Exit the application
            }

            // Check if the application is running as an administrator
            if (!IsRunningAsAdministrator())
            {
                Console.WriteLine("This application requires administrator privileges.");
                Console.WriteLine("Restarting with elevated privileges...");
                RestartAsAdministrator();
                Environment.Exit(0); // Exit the current process
            }
        }

        private static (string[] commands, int delayBetweenCommandsMs, int loopDelayMs) LoadCommandsToMemory()
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

            Console.WriteLine("Welcome to the EasyPC!\n");
            Console.WriteLine("\nThe app will start running commands automatically in the background.");
            Console.WriteLine("\nPress Ctrl+C to stop the application...\n");
            //Console.WriteLine("You can also enter an additional command at any time."); // suggest user input to run cmds

            if (string.IsNullOrEmpty(commands?.ToString()))
            {
                Console.WriteLine("\nThere is nothing to run: appsettings.json file is empty");
                Console.WriteLine("\nStopping the application...");
                Environment.Exit(0);
            }

            return (commands, delayBetweenCommandsMs, loopDelayMs);
        }

        static bool IsRunningAsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        static void RestartAsAdministrator()
        {
            ProcessStartInfo procInfo = new ProcessStartInfo
            {
                FileName = Process.GetCurrentProcess().MainModule?.FileName,
                Verb = "runas", // This triggers the UAC prompt
                UseShellExecute = true
            };

            try
            {
                Process.Start(procInfo);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                Console.WriteLine("The user declined to run the application as an administrator.");
                Console.WriteLine("Exiting...");
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

        static async Task HandleUserInputAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Console.Write("\nEnter an additional command to run (or press Enter to skip): ");
                string? additionalCommand = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(additionalCommand))
                {
                    try
                    {
                        Console.WriteLine($"Running additional command: {additionalCommand}");
                        await RunCommandAsync(additionalCommand, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An error occurred while running the additional command: {ex.Message}");
                    }
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
                    Console.Write("Output: ");
                    Console.WriteLine(output);
                }

                if (!string.IsNullOrEmpty(error))
                {
                    Console.Write("Error: ");
                    Console.WriteLine(error);
                }
            }
        }
    }
}


using System.Diagnostics;

namespace EasyPC
{
    class Program
    {
        // A flag to control the background thread
        private static bool _isRunning = true;

        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to the Automatic CMD Command Runner!");
            Console.WriteLine("The app will start running commands automatically in the background.");
            Console.WriteLine("Press Ctrl+C to stop the application...");

            // Start a background thread to run commands
            Thread commandThread = new Thread(RunCommands);
            commandThread.Start();

            // Handle graceful shutdown when the user presses Ctrl+C
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Console.WriteLine("\nStopping the application...");
                _isRunning = false; // Signal the thread to stop
                commandThread.Join(); // Wait for the thread to finish
                Console.WriteLine("Application stopped.");

                Environment.Exit(0);
            };
        }

        static void RunCommands()
        {
            while (_isRunning)
            {
                try
                {
                    RunCommand("net stop w3svc");
                    
                    Thread.Sleep(5000);

                    RunCommand("taskkill -f -im explorer.exe");

                    Thread.Sleep(10000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
        }

        static void RunCommand(string command)
        {
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

                // Read the output (if any)
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

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
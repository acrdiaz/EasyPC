

using System.Diagnostics;

namespace EasyPC
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Welcome to the CMD Command Runner!");
            Console.WriteLine("Available Commands:");
            Console.WriteLine("1. Stop IIS (net stop w3svc)");
            Console.WriteLine("2. Kill Explorer (taskkill -f -im explorer.exe)");
            Console.WriteLine("Enter the number of the command you want to run:");

            string choice = Console.ReadLine();

            string command;
            switch (choice)
            {
                case "1":
                    command = "net stop w3svc";
                    break;
                case "2":
                    command = "taskkill -f -im explorer.exe";
                    break;
                default:
                    Console.WriteLine("Invalid choice. Exiting...");
                    return;
            }

            try
            {
                RunCommand(command);
                Console.WriteLine($"Command executed successfully: {command}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        static void RunCommand(string command)
        {
            // Create a new process to run the command
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
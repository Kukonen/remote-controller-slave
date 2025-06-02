using RemoteControllerCore.Commands;
using RemoteControllerSlave.Models;
using System.Diagnostics;
using System.Text.Json;


namespace RemoteControllerSlave.Commands
{
    public class ConsoleCommandService
    {
        public CommandResult Execute(Command command)
        {
            var config = LoadConfig();

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/C {command.CommandText} {command.AdditionalInformationText}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = config.FilePath,
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                return new CommandResult
                {
                    IsSuccess = process.ExitCode == 0,
                    Result = output + error
                };
            }
            catch (Exception ex)
            {
                return new CommandResult 
                { 
                    IsSuccess = false, 
                    Result = ex.Message 
                };
            }
        }

        private CommandsConfig LoadConfig()
        {
            const string configPath = "commands_config.json";

            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException("Файл конфигурации команд не найден: " + configPath);
            }

            var json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<CommandsConfig>(json)!;
        }
    }
}

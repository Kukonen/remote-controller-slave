using RemoteControllerCore.Commands;
using RemoteControllerSlave.Models;
using System.Net;
using System.Text;
using System.Text.Json;


namespace RemoteControllerSlave.Commands
{
    public class HttpWorker : BackgroundService
    {
        private readonly ILogger<HttpWorker> _logger;
        private CommandsConfig _config;
        private readonly HttpCommandService _httpService;
        private readonly ConsoleCommandService _consoleService;

        public HttpWorker(
            ILogger<HttpWorker> logger,
            HttpCommandService httpService,
            ConsoleCommandService consoleService
        )
        {
            _logger = logger;
            _httpService = httpService;
            _consoleService = consoleService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            LoadConfig();

            var listener = new HttpListener();
            listener.Prefixes.Add($"http://*:{_config.Port}/");
            listener.Start();


            _logger.LogInformation($"HTTP server listening on port {_config.Port}");

            while (!stoppingToken.IsCancellationRequested)
            {
                var context = await listener.GetContextAsync();

                _ = Task.Run(async () =>
                {
                    var request = context.Request;
                    var response = context.Response;
                    string result = "";
                    int statusCode = 200;

                    if (request.HttpMethod == "POST")
                    {
                        using var inputStream = request.InputStream;
                        using var memoryStream = new MemoryStream();
                        await inputStream.CopyToAsync(memoryStream);
                        var body = Encoding.UTF8.GetString(memoryStream.ToArray());

                        try
                        {
                            var command = JsonSerializer.Deserialize<Command>(body)!;
                            CommandResult commandResult;

                            if (command.File?.Length > 0)
                            {
                                var path = Path.Combine(_config.FilePath, command.FileName);
                                await File.WriteAllBytesAsync(path, command.File);
                            }

                            switch (command.CommandType)
                            {
                                case CommandType.HTTP:
                                    commandResult = await _httpService.ExecuteAsync(command);
                                    break;
                                case CommandType.Console:
                                    commandResult = _consoleService.Execute(command);
                                    break;
                                default:
                                    throw new InvalidOperationException("Unsupported command type");
                            }

                            result = JsonSerializer.Serialize(commandResult);
                        }
                        catch (Exception ex)
                        {
                            statusCode = 500;
                            result = JsonSerializer.Serialize(new CommandResult
                            {
                                IsSuccess = false,
                                Result = ex.Message
                            });
                        }
                    }
                    else
                    {
                        statusCode = (int)HttpStatusCode.MethodNotAllowed;
                        result = "Method Not Allowed";
                    }

                    byte[] buffer = Encoding.UTF8.GetBytes(result);
                    response.StatusCode = statusCode;
                    response.ContentLength64 = buffer.Length;
                    await response.OutputStream.WriteAsync(buffer);
                    response.Close();

                }, stoppingToken);
            }

            listener.Stop();
        }

        private void LoadConfig()
        {
            const string configPath = "commands_config.json";

            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException("Файл конфигурации команд не найден: " + configPath);
            }

            var json = File.ReadAllText(configPath);
            _config = JsonSerializer.Deserialize<CommandsConfig>(json)!;
        }
    }
}

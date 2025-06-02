using RemoteControllerCore.Commands;
using System.Text;


namespace RemoteControllerSlave.Commands
{
    public class HttpCommandService
    {
        private readonly HttpClient _httpClient = new();
        private readonly ILogger<HttpCommandService> _logger;

        public HttpCommandService(ILogger<HttpCommandService> logger)
        {
            _logger = logger;
        }

        // парсится и выполняется запрос
        public async Task<CommandResult> ExecuteAsync(Command command)
        {
            try
            {
                var parameters = ParseAdditionalInfo(command.AdditionalInformationText);

                var request = new HttpRequestMessage(
                    new HttpMethod(parameters.Method),
                    command.CommandText
                );

                foreach (var header in parameters.Headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }

                if (!string.IsNullOrEmpty(parameters.Body))
                {
                    request.Content = new StringContent(parameters.Body, Encoding.UTF8, "application/json");
                }

                var response = await _httpClient.SendAsync(request);
                var resultContent = await response.Content.ReadAsStringAsync();

                return new CommandResult
                {
                    IsSuccess = true,
                    Result = resultContent
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP command execution failed");
                return new CommandResult
                {
                    IsSuccess = false,
                    Result = ex.Message
                };
            }
        }

        // Параметры парсятся, принимая в расчёт, что они находятся в формате для curl
        private (string Method, Dictionary<string, string> Headers, string Body) ParseAdditionalInfo(string text)
        {
            var lines = text.Split(';');
            var headers = new Dictionary<string, string>();
            string method = "GET";
            string body = "";

            foreach (var line in lines)
            {
                if (line.StartsWith("method=", StringComparison.OrdinalIgnoreCase))
                    method = line.Substring("method=".Length);
                else if (line.StartsWith("header=", StringComparison.OrdinalIgnoreCase))
                {
                    var headerLine = line.Substring("header=".Length);
                    var parts = headerLine.Split(':', 2);
                    if (parts.Length == 2)
                        headers[parts[0].Trim()] = parts[1].Trim();
                }
                else if (line.StartsWith("body=", StringComparison.OrdinalIgnoreCase))
                    body = line.Substring("body=".Length);
            }

            return (method, headers, body);
        }
    }

}

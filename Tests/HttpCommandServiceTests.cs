using Microsoft.Extensions.Logging;
using RemoteControllerCore.Commands;
using RemoteControllerSlave.Commands;


namespace Tests
{
    [TestClass]
    public class HttpCommandServiceTests
    {
        private HttpCommandService _service;

        [TestInitialize]
        public void Init()
        {
            var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<HttpCommandService>();
            _service = new HttpCommandService(logger);
        }

        [TestMethod]
        public async Task ValidHttpRequest()
        {
            var command = new Command
            {
                CommandType = CommandType.HTTP,
                CommandText = "https://ya.ru",
                AdditionalInformationText = ""
            };

            var result = await _service.ExecuteAsync(command);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(result.Result.Contains("яндекс"));
        }

        [TestMethod]
        public async Task InvalidUrlInHttpRequest()
        {
            var command = new Command
            {
                CommandType = CommandType.HTTP,
                CommandText = "http://nonexistent",
                AdditionalInformationText = "method=GET"
            };

            var result = await _service.ExecuteAsync(command);

            Assert.IsFalse(result.IsSuccess);
            Assert.IsTrue(result.Result.Length > 0);
        }
    }
}
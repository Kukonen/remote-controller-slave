using RemoteControllerCore.Commands;
using RemoteControllerSlave.Commands;
using System.IO;
using System.Text.Json;


namespace Tests
{
    [TestClass]
    public class ConsoleCommandServiceTests
    {
        private string _configPath = "commands_config.json";
        private ConsoleCommandService _service;

        [TestInitialize]
        public void Setup()
        {
            File.WriteAllText(_configPath, JsonSerializer.Serialize(new RemoteControllerSlave.Models.CommandsConfig
            {
                FilePath = Environment.CurrentDirectory,
                Port = "5000"
            }));

            _service = new ConsoleCommandService();
        }

        [TestCleanup]
        public void Cleanup()
        {
            File.Delete(_configPath);
        }

        [TestMethod]
        public void ValidCommand()
        {
            string text = "Hello, World!";

            var command = new Command
            {
                CommandType = CommandType.Console,
                CommandText = "echo",
                AdditionalInformationText = text
            };

            var result = _service.Execute(command);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsTrue(result.Result.Contains(text));
        }

        [TestMethod]
        public void InvalidCommand()
        {
            var command = new Command
            {
                CommandType = CommandType.Console,
                CommandText = "nothingcommand",
                AdditionalInformationText = ""
            };

            var result = _service.Execute(command);

            Assert.IsFalse(result.IsSuccess);
        }
    }
}

using RemoteControllerSlave.Commands;
using RemoteControllerSlave.Telemetry;


namespace RemoteControllerSlave
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Создание хост-приложения
            var builder = Host.CreateApplicationBuilder(args);

            // Команды
            builder.Services.AddSingleton<ConsoleCommandService>();
            builder.Services.AddSingleton<HttpCommandService>();
            builder.Services.AddHostedService<HttpWorker>();

            // OPC/SignalR сервис
            //builder.Services.AddHostedService<OpcUaWorker>();

            // Сборка и запуск всех фоновых служб
            var host = builder.Build();
            host.Run();
        }
    }
}
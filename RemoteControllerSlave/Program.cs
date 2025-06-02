using RemoteControllerSlave.Commands;
using RemoteControllerSlave.Telemetry;


namespace RemoteControllerSlave
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // �������� ����-����������
            var builder = Host.CreateApplicationBuilder(args);

            // �������
            builder.Services.AddSingleton<ConsoleCommandService>();
            builder.Services.AddSingleton<HttpCommandService>();
            builder.Services.AddHostedService<HttpWorker>();

            // OPC/SignalR ������
            //builder.Services.AddHostedService<OpcUaWorker>();

            // ������ � ������ ���� ������� �����
            var host = builder.Build();
            host.Run();
        }
    }
}
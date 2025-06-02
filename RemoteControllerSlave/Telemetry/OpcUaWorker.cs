using Microsoft.AspNetCore.SignalR.Client;
using Opc.Ua;
using Opc.Ua.Client;
using RemoteControllerCore.Telemetry;
using RemoteControllerSlave.Models;
using System.Text.Json;


namespace RemoteControllerSlave.Telemetry
{
    public class OpcUaWorker : BackgroundService
    {
        private readonly ILogger<OpcUaWorker> _logger;

        private HubConnection _signalRConnection;
        private Session _opcSession;
        private Subscription _subscription;
        private OpcConfig _opcConfig;

        public OpcUaWorker(ILogger<OpcUaWorker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            LoadOpcConfig();

            _signalRConnection = new HubConnectionBuilder()
                .WithUrl(_opcConfig.SignalRHubUrl)
                .WithAutomaticReconnect()
                .Build();

            await _signalRConnection.StartAsync(stoppingToken);

            // Настройка конфигурации OPC UA клиента
            var config = new ApplicationConfiguration
            {
                ApplicationName = "RemoteControllerSlave",
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = "Directory",
                        StorePath = "Certificates/Client",
                        SubjectName = "OpcUaClient"
                    },
                    AutoAcceptUntrustedCertificates = true,
                    AddAppCertToTrustedStore = false,
                    RejectSHA1SignedCertificates = false
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 }
            };

            var selectedEndpoint = CoreClientUtils.SelectEndpoint(_opcConfig.Endpoint, useSecurity: false);
            var endpointConfig = EndpointConfiguration.Create(config);
            var endpoint = new ConfiguredEndpoint(null, selectedEndpoint, endpointConfig);

            _opcSession = await Session.Create(config, endpoint, false, "OpcUaClient", 60000, null, null);

            _subscription = new Subscription(_opcSession.DefaultSubscription)
            {
                PublishingInterval = 150
            };

            foreach (var variable in _opcConfig.Variables)
            {
                // Опрашиваемый узел
                var nodeId = $"ns=2;s={_opcConfig.ObjectName}.{variable}";

                var item = new MonitoredItem(_subscription.DefaultItem)
                {
                    StartNodeId = new NodeId(nodeId),
                    AttributeId = Attributes.Value,
                    SamplingInterval = 1000
                };

                // Добовляем обовещения для отправки данных из OPC UA через SignalR
                item.Notification += async (monItem, args) =>
                {
                    if (args.NotificationValue is MonitoredItemNotification notification)
                    {
                        var value = notification.Value.WrappedValue.Value;
                        var payload = new TelemetryPayload
                        {
                            MachineId = Guid.Parse(_opcConfig.MachineId),
                            Variable = monItem.StartNodeId.Identifier.ToString(),
                            Value = value.ToString() ?? ""
                        };

                        _logger.LogInformation(JsonSerializer.Serialize(payload));

                        await _signalRConnection.InvokeAsync("SendData", _opcConfig.MachineId, payload, cancellationToken: stoppingToken);
                    }

                };

                _subscription.AddItem(item);
            }


            _opcSession.AddSubscription(_subscription);
            _subscription.Create();

            _logger.LogInformation("OPC UA соединение создано");
        }

        private void MonitoredItemHandler(MonitoredItem item, MonitoredItemNotificationEventArgs args)
        {
            try
            {
                Console.WriteLine("MonitoredItemHandler: " + item.StartNodeId.ToString());
                //OnMonitoredItemChanged?.Invoke(new clsOPCUAElement(item.StartNodeId.ToString(), item.LastValue, item.LastValue.GetType()));

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
            }
        }

        private void LoadOpcConfig()
        {
            const string configPath = "opc_config.json";

            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException("Файл конфигурации OPC UA не найден: " + configPath);
            }

            var json = File.ReadAllText(configPath);
            _opcConfig = JsonSerializer.Deserialize<OpcConfig>(json)!;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_subscription != null)
            {
                _subscription.Delete(true);
            }

            _opcSession?.Close();

            if (_signalRConnection != null)
            {
                await _signalRConnection.StopAsync(cancellationToken);
            }

            await base.StopAsync(cancellationToken);
        }
    }
}

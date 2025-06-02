namespace RemoteControllerSlave.Models
{
    public class OpcConfig
    {
        public string Endpoint { get; set; } = "";
        public string ObjectName { get; set; } = "";
        public List<string> Variables { get; set; } = new();
        public string MachineId { get; set; } = "";
        public string SignalRHubUrl { get; set; } = "";
    }
}

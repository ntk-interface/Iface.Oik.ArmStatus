using System.Text.Json.Nodes;

namespace Iface.Oik.ArmStatus;

public class WorkerConfig
{
    public string Worker { get; set; }
    public JsonNode Options { get; set; }
}

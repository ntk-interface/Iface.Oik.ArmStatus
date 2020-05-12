using Newtonsoft.Json.Linq;

namespace Iface.Oik.ArmStatus
{
  public class WorkerConfig
  {
    public string  Worker  { get; set; }
    public JObject Options { get; set; }
  }
}
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AElf.Configuration.Config.Management
{
    [ConfigFile(FileName = "deploy.json")]
    public class DeployConfig : ConfigBase<DeployConfig>
    {
        public string Type { get; set; }

        public bool Authentication { get; set; }

        public int SignTimeout { get; set; }
    }
}
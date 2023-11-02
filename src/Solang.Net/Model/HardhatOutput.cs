using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Solang
{
    public class HardhatOutput
    {
        [JsonPropertyName("input")] public HardhatOutputInput Input { get; set; }
    }

    public class HardhatOutputInput
    {
        [JsonPropertyName("sources")]
        public Dictionary<string, Dictionary<string, string>> Sources { get; set; }
    }
}
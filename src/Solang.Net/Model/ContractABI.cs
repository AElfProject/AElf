using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Solang
{
    public class ContractABI
    {
        [JsonPropertyName("authors")] public List<string> Authors { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; }
        [JsonPropertyName("version")] public string Version { get; set; }
    }
}
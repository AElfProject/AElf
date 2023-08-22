using System.Text.Json.Serialization;

namespace Solang
{
    public class SolangABI
    {
        [JsonPropertyName("contract")] public ContractABI Contract { get; set; }
        [JsonPropertyName("source")] public SourceABI Source { get; set; }
        [JsonPropertyName("spec")] public SpecABI Spec { get; set; }
        [JsonPropertyName("version")] public string Version { get; set; }
    }
}
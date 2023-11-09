using System.Text.Json.Serialization;

namespace Solang
{
    public class SourceABI
    {
        [JsonPropertyName("compiler")]
        public string Compiler { get; set; }
        [JsonPropertyName("hash")]
        public string Hash { get; set; }
        [JsonPropertyName("language")]
        public string Language { get; set; }
        [JsonPropertyName("wasm")]
        public string Wasm { get; set; }
    }
}
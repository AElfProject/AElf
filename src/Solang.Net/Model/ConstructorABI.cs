using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Solang
{
    public class ConstructorABI
    {
        [JsonPropertyName("args")] public List<ArgABI> Args { get; set; }
        [JsonPropertyName("default")] public bool Default { get; set; }
        [JsonPropertyName("docs")] public List<string> Docs { get; set; }
        [JsonPropertyName("label")] public string Label { get; set; }
        [JsonPropertyName("payable")] public bool Payable { get; set; }
        [JsonPropertyName("returnType")] public ReturnTypeABI ReturnType { get; set; }
        [JsonPropertyName("selector")] public string Selector { get; set; }
    }
}
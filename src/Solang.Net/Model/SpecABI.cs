using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Solang
{
    public class SpecABI
    {
        [JsonPropertyName("constructors")] public List<ConstructorABI> Constructors { get; set; }
        [JsonPropertyName("docs")] public List<string> Docs { get; set; }
        [JsonPropertyName("events")] public List<EventABI> Events { get; set; }
        [JsonPropertyName("messages")] public List<MessageABI> Messages { get; set; }
    }
}
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AElf.CLI.JS.Net
{
    public class HttpRequestModel
    {
        [JsonProperty("method")] public string Method { get; set; }
        [JsonProperty("url")] public string Url { get; set; }
        [JsonProperty("name")] public List<string> Params { get; set; }
    }
}
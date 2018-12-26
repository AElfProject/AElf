using Newtonsoft.Json;

namespace AElf.Management.Request
{
    public class JsonRpcArg
    {
        [JsonProperty("jsonrpc")] public string JsonRpc { get; set; }

        [JsonProperty("method")] public string Method { get; set; }

        [JsonProperty("id")] public int Id { get; set; }

        public JsonRpcArg()
        {
            JsonRpc = "2.0";
            Id = 1;
        }
    }

    public class JsonRpcArg<T>
    {
        [JsonProperty("jsonrpc")] public string JsonRpc { get; set; }

        [JsonProperty("method")] public string Method { get; set; }

        [JsonProperty("id")] public int Id { get; set; }

        [JsonProperty("params")] public T Params { get; set; }

        public JsonRpcArg()
        {
            JsonRpc = "2.0";
            Id = 1;
        }
    }

    public class JsonRpcResult<T>
    {
        [JsonProperty("jsonrpc")] public string JsonRpc { get; set; }

        [JsonProperty("id")] public int Id { get; set; }

        [JsonProperty("result")] public T Result { get; set; }
    }
}
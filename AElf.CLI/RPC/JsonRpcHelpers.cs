using Newtonsoft.Json.Linq;

namespace AElf.CLI.RPC
{
    public static class JsonRpcHelpers
    {
        public static JObject CreateRequest(JObject requestData, string method, int id)
        {
            JObject jObj = new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["method"] = method,
                ["params"] = requestData
            };

            return jObj;
        }
    }
}
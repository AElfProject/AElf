using Newtonsoft.Json.Linq;

namespace AElf.Kernel.Node.RPC
{
    public static class JsonRpcHelpers
    {
        public static JObject CreateResponse(JObject responseData, int id)
        {
            var reponse = new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = id,
                ["result"] = responseData
            };
            return reponse;
        }
    }
}
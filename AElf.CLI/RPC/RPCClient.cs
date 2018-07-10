using AElf.CLI.Http;
using Newtonsoft.Json.Linq;

namespace AElf.CLI.RPC
{
    public class RPCClient : IRPCClient
    {
        private readonly string _serverAddress;

        public RPCClient(string serverAddress)
        {
            _serverAddress = serverAddress;
        }

        public string Request(string method, JObject param = null)
        {
            if (param == null)
            {
                param = new JObject();
            }
            var req = new JObject
            {
                ["jsonrpc"] = "2.0",
                ["id"] = 0,
                ["method"] = method,
                ["params"] = param
            };
            return new HttpRequestor(_serverAddress).DoRequest(req.ToString());
        }
    }
}
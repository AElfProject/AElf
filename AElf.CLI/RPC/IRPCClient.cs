using Newtonsoft.Json.Linq;

namespace AElf.CLI.RPC
{
    public interface IRPCClient
    {
        string Request(string method, JObject param = null);
    }
}
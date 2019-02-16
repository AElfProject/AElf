using System.Threading.Tasks;
using AElf.Network;
using AElf.Network.Data;
using AElf.Network.Peers;
using AElf.RPC;
using Anemonis.AspNetCore.JsonRpc;
using Newtonsoft.Json.Linq;

namespace AElf.Net.Rpc
{
    [Path("/net")]
    public class NetRpcService : IJsonRpcService
    {
        public IPeerManager Manager { get; set; }
        public INetworkManager NetworkManager { get; set; }

        [JsonRpcMethod("GetPeers")]
        public async Task<JObject> GetPeers()
        {
            return await Manager.GetPeers();
        }

        [JsonRpcMethod("AddPeer", "address")]
        public async Task<bool> AddPeer(string address)
        {
            NodeData nodeData = null;

            try
            {
                nodeData = NodeData.FromString(address);
            }
            catch
            {
                // ignored
            }

            if (nodeData == null)
            {
                throw new JsonRpcServiceException(NetRpcErrorConsts.InvalidNetworkAddress,
                    NetRpcErrorConsts.RpcErrorMessage[NetRpcErrorConsts.InvalidNetworkAddress]);
            }

            await Task.Run(() => Manager.AddPeer(nodeData));

            return true;
        }

        [JsonRpcMethod("RemovePeer", "address")]
        public async Task<bool> RemovePeer(string address)
        {
            NodeData nodeData = null;

            try
            {
                nodeData = NodeData.FromString(address);
            }
            catch
            {
                // ignored
            }

            if (nodeData == null)
            {
                throw new JsonRpcServiceException(NetRpcErrorConsts.InvalidNetworkAddress,
                    NetRpcErrorConsts.RpcErrorMessage[NetRpcErrorConsts.InvalidNetworkAddress]);
            }

            await Task.Run(() => Manager.RemovePeer(nodeData));

            return true;
        }
    }
}
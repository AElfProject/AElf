using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Network.Data;
using AElf.Network.Peers.Exceptions;

namespace AElf.Network.Peers
{
    [LoggerName("NodeDialer")]
    public class NodeDialer : INodeDialer
    {
        public NodeData NodeData { get; private set; }

        public NodeDialer(NodeData _nodeData)
        {
            NodeData = _nodeData;
        }

        public async Task<IPeer> DialAsync(NodeData RemoteNodeData)
        {
            IPeer peer = new Peer(NodeData, RemoteNodeData);
            
            try
            {
                bool success = await peer.DoConnectAsync();

                if (success)
                    return peer;
            }
            catch (ResponseTimeOutException rex)
            {
                //_logger?.Error(rex, rex?.Message + " - "  + peer);
            }

            return null;
        }
    }
}
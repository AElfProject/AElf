
namespace AElf.Network.Data
{
    public partial class NodeData
    {
        public static NodeData FromString(string nodeDataStr)
        {
            if (string.IsNullOrEmpty(nodeDataStr))
                return null;
            
            string[] split = nodeDataStr.Split(':');

            if (split.Length != 2)
                return null;
                    
            ushort port = ushort.Parse(split[1]);
                    
            NodeData peer = new NodeData();
            peer.IpAddress = split[0];
            peer.Port = port;

            return peer;
        }
    }
}
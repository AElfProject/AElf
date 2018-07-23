using System.Text;

namespace AElf.Network.Data.Protobuf
{
    public partial class PeerListData
    {
        public string GetLoggerString()
        {
            if (NodeData.Count == 0)
                return "{ }";

            if (NodeData.Count == 1)
                return "{ " + NodeData[0].IpAddress + ":" + NodeData[0].Port + " }";
            
            StringBuilder strb = new StringBuilder();
            strb.Append("{ ");
            foreach (var peer in NodeData)
            {
                strb.Append(peer.IpAddress + ":" + peer.Port + ", ");
            }
            strb.Append(" }");

            return strb.ToString();
        }
    }
}
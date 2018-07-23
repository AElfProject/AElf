using System;
using System.Security.Cryptography;

namespace AElf.Network.Data.Protobuf
{
    public partial class NodeData
    {
     //   public bool IsBootnode { get; set; } = false;

        public static Protobuf.NodeData FromString(string nodeDataStr)
        {
            if (string.IsNullOrEmpty(nodeDataStr))
                return null;
            
            string[] split = nodeDataStr.Split(':');

            if (split.Length != 2)
                return null;
                    
            ushort port = ushort.Parse(split[1]);
                    
            Protobuf.NodeData peer = new Protobuf.NodeData();
            peer.IpAddress = split[0];
            peer.Port = port;

            return peer;
        }
    }
}
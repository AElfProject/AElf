using System;
using AElf.Network.Data;

namespace AElf.Network.Connection
{
    public class Message
    {
        public int Type { get; set; } 
        public bool IsConsensus { get; set; }
        public int Length { get; set; } 
        
        public byte[] Payload { get; set; }
        
        public string OutboundTrace { get; set; }

        public override string ToString()
        {
            return $"{{ message - type: {(MessageType) Type}, length: {Length}, payload-length: {Payload?.Length}}}";
        }
    }
}
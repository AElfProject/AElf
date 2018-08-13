using AElf.Network.Data;

namespace AElf.Network.Connection
{
    public class Message
    {
        public int Type { get; set; } 
        
        public bool HasId { get; set; }
        public byte[] Id { get; set; }
        
        public int Length { get; set; } 
        public byte[] Payload { get; set; }
        
        public string OutboundTrace { get; set; } // todo remove

        public override string ToString()
        {
            return $"{{ message - type: {(MessageType) Type}, length: {Length}, payload-length: {Payload?.Length}}}";
        }
        
        // todo remove : really bad design
        public string RequestLogString { get; set; }
    }
}
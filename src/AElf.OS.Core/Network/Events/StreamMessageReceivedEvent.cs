using Google.Protobuf;

namespace AElf.OS.Network.Events;

public class StreamMessageReceivedEvent
{
    public StreamMessageReceivedEvent(ByteString message, string clientPubkey, string requestId)
    {
        Message = message;
        ClientPubkey = clientPubkey;
        RequestId = requestId;
    }

    public ByteString Message { get; }

    public string ClientPubkey { get; }

    public string RequestId { get; }
}
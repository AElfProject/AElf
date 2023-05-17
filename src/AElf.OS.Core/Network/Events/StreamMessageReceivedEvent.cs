using Google.Protobuf;

namespace AElf.OS.Network.Events;

public class StreamMessageReceivedEvent
{
    public StreamMessageReceivedEvent(ByteString message, string clientPubkey)
    {
        Message = message;
        ClientPubkey = clientPubkey;
    }

    public ByteString Message { get; }

    public string ClientPubkey { get; }
}
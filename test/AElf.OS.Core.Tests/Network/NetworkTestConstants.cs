namespace AElf.OS.Network
{
    public class NetworkTestConstants
    {
        public const string FakeIpEndpoint = "127.0.0.1:666";
        public const string FakeIpEndpoint2 = "127.0.0.1:1000";
        
        public const string DialExceptionIpEndpoint = "127.0.0.1:1234";
        public const string HandshakeWithNetExceptionIp = "127.0.0.1:1235";
        public const string BadHandshakeIp = "127.0.0.1:1236";
        
        public const string GoodPeerEndpoint = "127.0.0.1:1335";
        
        public const int DefaultChainId = 1;
        
        public const string FakePubkey = "048f5ced21f8d687cb9ade1c22dc0e183b05f87124c82073f5d82a09b139cc466efbfb6f28494d0a9d7366fcb769fe5436cfb7b5d322a2b0f69c4bcb1c33ac24ad";
        public const string FakePubkey2 = "040a7bf44d2c79fe5e270943773783a24eed5cda3e71fa49470cdba394a23832d5c831e233cddebea2720c194dffadd656d4dedf84643818ca77edeee17ad4307a";
    }
}
using System.Threading.Tasks;
using AElf.OS.Network.Helpers;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Grpc;

public class PeerDialerReplyErrorTests : PeerDialerReplyErrorTestBase
{
    private readonly IPeerDialer _peerDialer;

    public PeerDialerReplyErrorTests()
    {
        _peerDialer = GetRequiredService<IPeerDialer>();
    }

    [Fact]
    public async Task DialPeer_Test()
    {
        AElfPeerEndpointHelper.TryParse("127.0.0.1:2000", out var endpoint);
        var grpcPeer = await _peerDialer.DialPeerAsync(endpoint);

        grpcPeer.ShouldBeNull();
    }
}
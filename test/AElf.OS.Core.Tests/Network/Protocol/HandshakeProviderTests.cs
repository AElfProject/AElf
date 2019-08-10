using System.Threading.Tasks;
using AElf.Kernel.Account.Application;
using AElf.OS.Network.Infrastructure;
using Shouldly;
using Xunit;

namespace AElf.OS.Network.Protocol
{
    public class HandshakeProviderTests : NetworkInfrastructureTestBase
    {
        private readonly IHandshakeProvider _handshakeProvider;
        private readonly IAccountService _accountService;

        public HandshakeProviderTests()
        {
            _handshakeProvider = GetRequiredService<IHandshakeProvider>();
            _accountService = GetRequiredService<IAccountService>();
        }

        [Fact]
        public async Task GetHandshakeAsync_Test()
        {
            var handshake = await _handshakeProvider.GetHandshakeAsync();
            handshake.ShouldNotBeNull();
            handshake.HandshakeData.Pubkey.ShouldBe(await _accountService.GetPublicKeyAsync());
        }
    }
}
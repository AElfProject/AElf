using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using Google.Protobuf;
using Microsoft.Extensions.Options;

namespace AElf.OS.Network.Grpc
{
    public class ConnectionInfoProvider : IConnectionInfoProvider
    {
        private ChainOptions ChainOptions => ChainOptionsSnapshot.Value;
        public IOptionsSnapshot<ChainOptions> ChainOptionsSnapshot { get; set; }
        private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
        public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }
        
        private readonly IAccountService _accountService;

        public ConnectionInfoProvider(IAccountService accountService)
        {
            _accountService = accountService;
        }

        public async Task<ConnectionInfo> GetConnectionInfoAsync()
        {
            return new ConnectionInfo
            {
                ChainId = ChainOptions.ChainId,
                ListeningPort = NetworkOptions.ListeningPort,
                Version = KernelConstants.ProtocolVersion,
                Pubkey = ByteString.CopyFrom(await _accountService.GetPublicKeyAsync())
            };
        }
    }
}
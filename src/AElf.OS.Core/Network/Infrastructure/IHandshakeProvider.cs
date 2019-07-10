using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Options;

namespace AElf.OS.Network.Infrastructure
{
    public interface IHandshakeProvider
    {
        Task<Handshake> GetHandshakeAsync();
    }

    public class HandshakeProvider : IHandshakeProvider
    {
        private NetworkOptions NetworkOptions => NetworkOptionsSnapshot.Value;
        public IOptionsSnapshot<NetworkOptions> NetworkOptionsSnapshot { get; set; }
        
        private readonly IAccountService _accountService;
        private readonly IBlockchainService _blockchainService;

        public HandshakeProvider(IAccountService accountService, IBlockchainService blockchainService)
        {
            _accountService = accountService;
            _blockchainService = blockchainService;
        }

        public async Task<Handshake> GetHandshakeAsync()
        {
            var nd = new HandshakeData
            {
                ListeningPort = NetworkOptions.ListeningPort,
                Pubkey = ByteString.CopyFrom(await _accountService.GetPublicKeyAsync()),
                Version = KernelConstants.ProtocolVersion,
                ChainId = _blockchainService.GetChainId()
            };

            byte[] sig = await _accountService.SignAsync(Hash.FromMessage(nd).ToByteArray());

            var chain = await _blockchainService.GetChainAsync();
                
            var hsk = new Handshake
            {
                HandshakeData = nd,
                Signature = ByteString.CopyFrom(sig),
                BestChainBlockHeader = await _blockchainService.GetBestChainLastBlockHeaderAsync(),
                LibBlockHeight = chain?.LastIrreversibleBlockHeight ?? 0
            };

            return hsk;
        }
    }
}
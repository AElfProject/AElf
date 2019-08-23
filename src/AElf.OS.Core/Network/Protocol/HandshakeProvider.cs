using System.Threading.Tasks;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Google.Protobuf;

namespace AElf.OS.Network.Protocol
{
    public class HandshakeProvider : IHandshakeProvider
    {
        private readonly IAccountService _accountService;
        private readonly IBlockchainService _blockchainService;

        public HandshakeProvider(IAccountService accountService, IBlockchainService blockchainService)
        {
            _accountService = accountService;
            _blockchainService = blockchainService;
        }

        public async Task<Handshake> GetHandshakeAsync()
        {
            var chain = await _blockchainService.GetChainAsync();

            var nd = new HandshakeData
            {
                Pubkey = ByteString.CopyFrom(await _accountService.GetPublicKeyAsync()),
                BestChainHead = await _blockchainService.GetBestChainLastBlockHeaderAsync(),
                LibBlockHeight = chain?.LastIrreversibleBlockHeight ?? 0
            };

            byte[] sig = await _accountService.SignAsync(Hash.FromMessage(nd).ToByteArray());
                
            var hsk = new Handshake
            {
                HandshakeData = nd,
                Signature = ByteString.CopyFrom(sig)
            };

            return hsk;
        }
    }
}
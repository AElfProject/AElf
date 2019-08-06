using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Network.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.Network.Protocol
{
    public class HandshakeProvider : IHandshakeProvider
    {
        private readonly IAccountService _accountService;
        private readonly IBlockchainService _blockchainService;
        
        public ILogger<HandshakeProvider> Logger { get; set; }

        public HandshakeProvider(IAccountService accountService, IBlockchainService blockchainService)
        {
            _accountService = accountService;
            _blockchainService = blockchainService;
            
            Logger = NullLogger<HandshakeProvider>.Instance;
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

        public Task<bool> ValidateHandshakeAsync(Handshake handshake, string connectionPubkey)
        {
            if (handshake?.HandshakeData == null)
            {
                Logger.LogWarning("Handshake is null.");
                return Task.FromResult(false);
            }

            if (handshake.HandshakeData.Pubkey.ToHex() != connectionPubkey)
            {
                Logger.LogWarning("Handshake pubkey is incorrect.");
                return Task.FromResult(false);
            }
            
            var validData = CryptoHelper.VerifySignature(handshake.Signature.ToByteArray(),
                Hash.FromMessage(handshake.HandshakeData).ToByteArray(), handshake.HandshakeData.Pubkey.ToByteArray());

            if (!validData)
            {
                Logger.LogWarning("Handshake signature is incorrect.");
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }
    }
}
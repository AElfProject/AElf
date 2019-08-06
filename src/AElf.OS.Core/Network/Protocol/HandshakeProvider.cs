using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel;
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
                LibBlockHeight = chain.LastIrreversibleBlockHeight,
                Time = TimestampHelper.GetUtcNow()
            };

            var signature = await _accountService.SignAsync(Hash.FromMessage(nd).ToByteArray());
                
            var handshake = new Handshake
            {
                HandshakeData = nd,
                Signature = ByteString.CopyFrom(signature)
            };

            return handshake;
        }

        public Task<bool> ValidateHandshakeAsync(Handshake handshake)
        {
            if (handshake?.HandshakeData == null)
            {
                Logger.LogWarning("Handshake is null.");
                return Task.FromResult(false);
            }

            if (TimestampHelper.GetUtcNow() > handshake.HandshakeData.Time +
                TimestampHelper.DurationFromMilliseconds(NetworkConstants.HandshakeTimeout))
            {
                Logger.LogWarning("Handshake is expired.");
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
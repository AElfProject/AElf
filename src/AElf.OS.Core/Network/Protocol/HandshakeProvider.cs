using System.Security.Cryptography;
using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AElf.OS.Network.Protocol
{
    public class HandshakeProvider : IHandshakeProvider
    {
        private readonly IAccountService _accountService;
        private readonly IBlockchainService _blockchainService;
        private readonly NetworkOptions _networkOptions;

        public ILogger<HandshakeProvider> Logger { get; set; }

        public HandshakeProvider(IAccountService accountService, IBlockchainService blockchainService,
            IOptionsSnapshot<NetworkOptions> networkOptions)
        {
            _accountService = accountService;
            _blockchainService = blockchainService;
            _networkOptions = networkOptions.Value;

            Logger = NullLogger<HandshakeProvider>.Instance;
        }

        public async Task<Handshake> GetHandshakeAsync()
        {
            var chain = await _blockchainService.GetChainAsync();

            var handshakeData = new HandshakeData
            {
                ChainId = chain.Id,
                Version = KernelConstants.ProtocolVersion,
                ListeningPort = _networkOptions.ListeningPort,
                Pubkey = ByteString.CopyFrom(await _accountService.GetPublicKeyAsync()),
                BestChainHash = chain.BestChainHash,
                BestChainHeight = chain.BestChainHeight,
                LastIrreversibleBlockHash = chain.LastIrreversibleBlockHash,
                LastIrreversibleBlockHeight = chain.LastIrreversibleBlockHeight,
                Time = TimestampHelper.GetUtcNow()
            };

            var signature = await _accountService.SignAsync(HashHelper.ComputeFromMessage(handshakeData).ToByteArray());

            var handshake = new Handshake
            {
                HandshakeData = handshakeData,
                SessionId = ByteString.CopyFrom(GenerateRandomToken()),
                Signature = ByteString.CopyFrom(signature)
            };

            return handshake;
        }
        
        public static byte[] GenerateRandomToken()
        {
            byte[] sessionId = new byte[NetworkConstants.DefaultSessionIdSize];

            using (RNGCryptoServiceProvider rg = new RNGCryptoServiceProvider()) 
            { 
                rg.GetBytes(sessionId);
            }

            return sessionId;
        }

        public async Task<HandshakeValidationResult> ValidateHandshakeAsync(Handshake handshake)
        {
            var pubkey = handshake.HandshakeData.Pubkey.ToHex();
            if (_networkOptions.AuthorizedPeers == AuthorizedPeers.Authorized &&
                !_networkOptions.AuthorizedKeys.Contains(pubkey))
            {
                return HandshakeValidationResult.Unauthorized;
            }

            var chainId = _blockchainService.GetChainId();
            if (handshake.HandshakeData.ChainId != chainId)
            {
                Logger.LogWarning($"Chain is is incorrect: {handshake.HandshakeData.ChainId}.");
                return HandshakeValidationResult.InvalidChainId;
            }

            if (handshake.HandshakeData.Version != KernelConstants.ProtocolVersion)
            {
                Logger.LogWarning($"Version is is incorrect: {handshake.HandshakeData.Version}.");
                return HandshakeValidationResult.InvalidVersion;
            }

            var now = TimestampHelper.GetUtcNow();
            if (now > handshake.HandshakeData.Time +
                TimestampHelper.DurationFromMilliseconds(NetworkConstants.HandshakeTimeout))
            {
                Logger.LogWarning($"Handshake is expired: {handshake.HandshakeData.Time}, reference now: {now}.");
                return HandshakeValidationResult.HandshakeTimeout;
            }

            byte[] handshakePubkey = handshake.HandshakeData.Pubkey.ToByteArray();
            
            var validData = CryptoHelper.VerifySignature(handshake.Signature.ToByteArray(),
                HashHelper.ComputeFromMessage(handshake.HandshakeData).ToByteArray(), handshakePubkey);

            if (!validData)
            {
                Logger.LogWarning("Handshake signature is incorrect.");
                return HandshakeValidationResult.InvalidSignature;
            }

            var nodePubKey = await _accountService.GetPublicKeyAsync();
            if (handshakePubkey.BytesEqual(nodePubKey))
            {
                Logger.LogWarning("Self connection detected.");
                return HandshakeValidationResult.SelfConnection;
            }

            return HandshakeValidationResult.Ok;
        }
    }

    public enum HandshakeValidationResult
    {
        Ok = 0,
        InvalidChainId = 1,
        InvalidVersion = 2,
        HandshakeTimeout = 3,
        InvalidSignature = 4,
        Unauthorized = 5,
        SelfConnection = 6
    }
}
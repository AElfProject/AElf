using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography;
using AElf.Kernel.Account.Application;

namespace AElf.Contracts.Consensus.DPoS
{
    public class AccountService : IAccountService
    {
        private readonly IECKeyPairProvider _ecKeyPairProvider;

        public AccountService(IECKeyPairProvider ecKeyPairProvider)
        {
            _ecKeyPairProvider = ecKeyPairProvider;
        }

        public async Task<byte[]> SignAsync(byte[] data)
        {
            var signature = CryptoHelpers.SignWithPrivateKey(_ecKeyPairProvider.GetECKeyPair().PrivateKey, data);
            return signature;
        }

        public async Task<bool> VerifySignatureAsync(byte[] signature, byte[] data, byte[] publicKey)
        {
            var recoverResult = CryptoHelpers.RecoverPublicKey(signature, data, out var recoverPublicKey);
            return recoverResult && publicKey.BytesEqual(recoverPublicKey);
        }

        public Task<byte[]> GetPublicKeyAsync()
        {
            return Task.FromResult(_ecKeyPairProvider.GetECKeyPair().PublicKey);
        }

        public Task<byte[]> EncryptMessage(byte[] receiverPublicKey, byte[] plainMessage)
        {
            return Task.FromResult(CryptoHelpers.EncryptMessage(_ecKeyPairProvider.GetECKeyPair().PrivateKey,
                receiverPublicKey, plainMessage));
        }

        public Task<byte[]> DecryptMessage(byte[] senderPublicKey, byte[] cipherMessage)
        {
            return Task.FromResult(CryptoHelpers.DecryptMessage(senderPublicKey,
                _ecKeyPairProvider.GetECKeyPair().PublicKey, cipherMessage));
        }
    }
}
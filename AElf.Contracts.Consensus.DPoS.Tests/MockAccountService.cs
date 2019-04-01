using System.Threading.Tasks;
using AElf.Common;
using AElf.Cryptography;
using AElf.Kernel.Account.Application;

namespace AElf.Contracts.Consensus.DPoS
{
    /// <summary>
    /// Really need this service to provide key pairs during dpos consensus testing.
    /// </summary>
    public class MockAccountService : IAccountService
    {
        private readonly IECKeyPairProvider _ecKeyPairProvider;

        public MockAccountService(IECKeyPairProvider ecKeyPairProvider)
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

        public async Task<byte[]> GetPublicKeyAsync()
        {
            return _ecKeyPairProvider.GetECKeyPair().PublicKey;
        }

        public async Task<byte[]> EncryptMessage(byte[] receiverPublicKey, byte[] plainMessage)
        {
            return CryptoHelpers.EncryptMessage(_ecKeyPairProvider.GetECKeyPair().PrivateKey, receiverPublicKey,
                plainMessage);
        }

        public async Task<byte[]> DecryptMessage(byte[] senderPublicKey, byte[] cipherMessage)
        {
            return CryptoHelpers.DecryptMessage(senderPublicKey, _ecKeyPairProvider.GetECKeyPair().PrivateKey,
                cipherMessage);
        }
    }
}
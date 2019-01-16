using System.Threading.Tasks;
using AElf.Common;
using AElf.Common.Application;
using AElf.Configuration;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;

namespace AElf.Kernel.Services
{
    public class AccountService : IAccountService
    {
        private readonly ECKeyPair _accountKeyPair;

        public AccountService()
        {
            var keyStore = new AElfKeyStore(ApplicationHelpers.ConfigPath);
            keyStore.OpenAsync(NodeConfig.Instance.NodeAccount, NodeConfig.Instance.NodeAccountPassword, false).Wait();
            _accountKeyPair = keyStore.GetAccountKeyPair(NodeConfig.Instance.NodeAccount);
        }

        public AccountService(ECKeyPair accountKeyPair)
        {
            _accountKeyPair = accountKeyPair;
        }

        public async Task<byte[]> Sign(byte[] data)
        {
            var signer = new ECSigner();
            var signature = signer.Sign(_accountKeyPair, data);

            return signature.SigBytes;
        }

        public async Task<bool> VerifySignature(byte[] signatureBytes, byte[] data)
        {
            var verifier = new ECVerifier();
            var result = verifier.Verify(new ECSignature(signatureBytes),data);
            
            return result;
        }

        public async Task<byte[]> GetPublicKey()
        {
            return _accountKeyPair.PublicKey;
        }

        public async Task<Address> GetAddress()
        {
            return Address.FromPublicKey(_accountKeyPair.PublicKey);
        }
    }
}
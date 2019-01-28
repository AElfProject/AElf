using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.OS.Network.Temp;
using Moq;

namespace AElf.OS.Tests.Network
{
    public static class NetMockHelpers
    {
        /// <summary>
        /// Builds a basic account service.
        /// </summary>
        /// <returns>the mocked account service</returns>
        public static Mock<IAccountService> MockAccountService()
        {
            var keyPair = new KeyPairGenerator().Generate();
            
            var accountService = new Mock<IAccountService>();
            
            accountService.Setup(m => m.GetPublicKey()).Returns(Task.FromResult(keyPair.PublicKey));
            
            accountService
                .Setup(m => m.Sign(It.IsAny<byte[]>()))
                .Returns<byte[]>(m => Task.FromResult(new ECSigner().Sign(keyPair, m).SigBytes));
            
            accountService
                .Setup(m => m.VerifySignature(It.IsAny<byte[]>(), It.IsAny<byte[]>()))
                .Returns<byte[], byte[]>( (sig, data) => Task.FromResult(CryptoHelpers.Verify(sig, data, keyPair.PublicKey)));

            return accountService;
        }
    }
}
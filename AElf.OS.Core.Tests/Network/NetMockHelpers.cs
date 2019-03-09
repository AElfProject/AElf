using System.Threading.Tasks;
using AElf.Cryptography;
using AElf.Kernel.Account.Application;
using Moq;

namespace AElf.OS.Network
{
    public static class NetMockHelpers
    {
        /// <summary>
        /// Builds a basic account service.
        /// </summary>
        public static Mock<IAccountService> MockAccountService()
        {
            var keyPair = CryptoHelpers.GenerateKeyPair();
            var accountService = new Mock<IAccountService>();
            
            accountService.Setup(m => m.GetPublicKeyAsync()).Returns(Task.FromResult(keyPair.PublicKey));
            
            accountService
                .Setup(m => m.SignAsync(It.IsAny<byte[]>()))
                .Returns<byte[]>(m => Task.FromResult(CryptoHelpers.SignWithPrivateKey(keyPair.PrivateKey, m)));
            
            accountService
                .Setup(m => m.VerifySignatureAsync(It.IsAny<byte[]>(), It.IsAny<byte[]>(), It.IsAny<byte[]>()))
                .ReturnsAsync(true);

            return accountService;
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Economic.TestBase;
using AElf.Cryptography;
using AElf.Cryptography.SecretSharing;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSTest
    {
        private static int MinimumCount => EconomicContractsTestConstants.InitialCoreDataCenterCount.Mul(2).Div(3);

        /// <summary>
        /// Test the correctness of basic logic of secret sharing.
        /// </summary>
        /// <returns></returns>
        [Fact]
        public void OffChain_DecryptMessage_Test()
        {
            var message = HashHelper.ComputeFrom("message").ToByteArray();
            var secrets =
                SecretSharingHelper.EncodeSecret(message, MinimumCount, EconomicContractsTestConstants.InitialCoreDataCenterCount);
            var encryptedValues = new Dictionary<string, byte[]>();
            var decryptedValues = new Dictionary<string, byte[]>();
            var ownerKeyPair = InitialCoreDataCenterKeyPairs[0];
            var othersKeyPairs = InitialCoreDataCenterKeyPairs.Skip(1).ToList();
            var decryptResult=new byte[0];

            var initial = 0;
            foreach (var keyPair in othersKeyPairs)
            {
                var encryptedMessage = CryptoHelper.EncryptMessage(ownerKeyPair.PrivateKey, keyPair.PublicKey,
                    secrets[initial++]);
                encryptedValues.Add(keyPair.PublicKey.ToHex(), encryptedMessage);
            }

            // Check encrypted values.
            encryptedValues.Count.ShouldBe(EconomicContractsTestConstants.InitialCoreDataCenterCount - 1);

            // Others try to recover.
            foreach (var keyPair in othersKeyPairs)
            {
                var cipherMessage = encryptedValues[keyPair.PublicKey.ToHex()];
                var decryptMessage =
                    CryptoHelper.DecryptMessage(ownerKeyPair.PublicKey, keyPair.PrivateKey, cipherMessage);
                decryptedValues.Add(keyPair.PublicKey.ToHex(), decryptMessage);

                if (decryptedValues.Count >= MinimumCount)
                {
                    decryptResult = SecretSharingHelper.DecodeSecret(
                        decryptedValues.Values.ToList(),
                        Enumerable.Range(1, MinimumCount).ToList(), MinimumCount);
                    break;
                }
            }

            decryptResult.ShouldBe(message);
        }
    }
}
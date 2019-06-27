using AElf.Cryptography;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Types.Tests
{
    public class ChainTest
    {
        [Fact]
        public void Basic_ChainTest()
        {
            var chainId = 2111;
            var chain = new Chain(chainId, Hash.Empty);
            var serializeData = chain.Serialize();
        }

        [Fact]
        public void GetDisambiguationHashTest()
        {
            var blockHeight = 10;
            var keyPair = CryptoHelpers.GenerateKeyPair();
            var pubKeyHash = Hash.FromRawBytes(keyPair.PublicKey);
        }
    }
}
using AElf.Common;
using AElf.Cryptography;
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
            var chain = new Chain(ChainHelpers.GetRandomChainId(), Hash.Empty);
            var serializeData = chain.Serialize();
        }

        [Fact]
        public void GetDisambiguationHashTest()
        {
            var blockHeight = 10;
            var keyPair = CryptoHelpers.GenerateKeyPair();
            var pubKeyHash = Hash.FromRawBytes(keyPair.PublicKey);
            var hash = HashHelpers.GetDisambiguationHash(blockHeight, pubKeyHash);
            hash.ShouldNotBeNull();
            hash.ShouldNotBe(Hash.FromMessage(new Int64Value(){Value = blockHeight}));
            hash.ShouldNotBe(pubKeyHash);
        }
    }
}
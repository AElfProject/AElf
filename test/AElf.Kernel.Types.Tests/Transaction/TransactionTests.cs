using AElf.Common;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Types.Tests
{
    public class TransactionTests
    {
        [Fact]
        public void Transaction_HashTest()
        {
            var transaction = new Transaction
            {
                From = Address.Generate(),
                To = Address.Genesis,
                MethodName = "TestMethod",

            };
            var hash = transaction.GetHash();
            var hashBytes = transaction.GetHashBytes();
            var hash1 = Hash.LoadByteArray(hashBytes);
            hash.ShouldBe(hash1);
        }

        [Fact]
        public void TransactionResult_SerializeTest()
        {
            var transactionResult = new TransactionResult()
            {
                Status = TransactionResultStatus.Mined,
                BlockHash = Hash.Generate(),
                BlockNumber = 10,
                ReturnValue = new StringValue{Value = "test"}.ToByteString()
            };

            var serializeData = transactionResult.Serialize();
            serializeData.ShouldNotBeNull();

            var transactionResult1 = TransactionResult.Parser.ParseFrom(serializeData);
            transactionResult.ShouldBe(transactionResult1);
        }
    }
}
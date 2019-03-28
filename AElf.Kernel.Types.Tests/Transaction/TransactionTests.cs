using AElf.Common;
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
                MethodName = "TestMethod"
            };
            var hash = transaction.GetHash();
            var hashBytes = transaction.GetHashBytes();
            var hash1 = Hash.LoadByteArray(hashBytes);
            hash.ShouldBe(hash1);
        }
    }
}
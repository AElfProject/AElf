using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel
{
    public class BlockBodyExtensionsTest : AElfKernelTestBase
    {
        private readonly KernelTestHelper _kernelTestHelper;

        public BlockBodyExtensionsTest()
        {
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }

        [Fact]
        public void BlockBody_Test()
        {
            var blockBody = new BlockBody();
            blockBody.CalculateMerkleTreeRoot().ShouldBe(Hash.Empty);

            var transaction1 = _kernelTestHelper.GenerateTransaction();
            var transaction2 = _kernelTestHelper.GenerateTransaction();
            var transaction3 = _kernelTestHelper.GenerateTransaction();

            blockBody.AddTransaction(transaction1);
            blockBody.TransactionIds.Count.ShouldBe(1);
            blockBody.TransactionIds.ShouldContain(transaction1.GetHash());

            blockBody.AddTransactions(new[] {transaction2.GetHash(), transaction3.GetHash()});
            blockBody.TransactionIds.Count.ShouldBe(3);
            blockBody.TransactionIds.ShouldContain(transaction2.GetHash());
            blockBody.TransactionIds.ShouldContain(transaction3.GetHash());
            
            blockBody.CalculateMerkleTreeRoot().ShouldBe(BinaryMerkleTree.FromLeafNodes(blockBody.TransactionIds).Root);
        }
    }
}
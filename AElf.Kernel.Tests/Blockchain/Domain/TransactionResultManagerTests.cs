//using System.Threading.Tasks;
//using AElf.Common;
//using Shouldly;
//using Xunit;
//
//namespace AElf.Kernel.Blockchain.Domain
//{
//    public class TransactionResultManagerTests : AElfKernelTestBase
//    {
//        private readonly TransactionResultManager _transactionResultManager;
//
//        public TransactionResultManagerTests()
//        {
//            _transactionResultManager = GetRequiredService<TransactionResultManager>();
//        }
//
//        [Fact]
//        public async Task Add_TransactionResult_Success()
//        {
//            var transactionResult = new TransactionResult
//            {
//                TransactionId = Hash.FromString("New TransactionResult")
//            };
//            await _transactionResultManager.AddTransactionResultAsync(transactionResult);
//
//            var result = await _transactionResultManager.GetTransactionResultAsync(transactionResult.TransactionId);
//
//            result.ShouldNotBeNull();
//            result.TransactionId.ShouldBe(transactionResult.TransactionId);
//        }
//
//        [Fact]
//        public async Task Get_TransactionResult_ReturnNull()
//        {
//            var result =
//                await _transactionResultManager.GetTransactionResultAsync(
//                    Hash.FromString("Not Exist TransactionResult"));
//
//            result.ShouldBeNull();
//        }
//    }
//}
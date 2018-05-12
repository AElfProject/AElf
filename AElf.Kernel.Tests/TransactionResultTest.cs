using System.Threading.Tasks;
using AElf.Kernel.Managers;
using AElf.Kernel.Services;
using Xunit;

namespace AElf.Kernel.Tests
{
    public class TransactionResultTest
    {
        private readonly ITransactionResultService _transactionResultService;
        private readonly ITransactionResultManager _transactionResultManager;

        public TransactionResultTest(ITransactionResultService transactionResultService, ITransactionResultManager transactionResultManager)
        {
            _transactionResultService = transactionResultService;
            _transactionResultManager = transactionResultManager;
        }

        private TransactionResult CreateResult(Hash txId, Status status)
        {
            return new TransactionResult
            {
                TransactionId = txId,
                Status = status
            };
        }

        
        [Fact]
        public async Task TxResultStorage()
        {
            var txId = Hash.Generate();
            var res = CreateResult(txId, Status.Mined);
            await _transactionResultManager.AddTransactionResultAsync(res);
            var r = await _transactionResultManager.GetTransactionResultAsync(txId);
            Assert.Equal(res, r);
        }
        
        [Fact]
        public async Task AddTxResult()
        {
            var txId = Hash.Generate();
            var res = new TransactionResult
            {
                TransactionId = txId,
                Status = Status.Mined
            };
            await _transactionResultService.AddResultAsync(res);
        }
        
        [Fact]
        public async Task GetTxResultNotExisted()
        {
            var txId = Hash.Generate();
            var res = await _transactionResultService.GetResultAsync(txId);
            Assert.Equal(txId, res.TransactionId);
            Assert.Equal(res.Status, Status.NotExisted);
        }

        [Fact]
        public async Task GetTxResultInStorage()
        {
            var txId = Hash.Generate();
            var res = CreateResult(txId, Status.Mined);
            await _transactionResultManager.AddTransactionResultAsync(res);

            var r =await _transactionResultService.GetResultAsync(txId);
            Assert.Equal(res, r);
        }

    }
}
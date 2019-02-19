//TODO:! move to other project
/*
using System;
using System.Threading.Tasks;
using AElf.Kernel.Managers;
using AElf.Kernel.ChainController;
using AElf.Kernel.ChainController.Rpc;
using Xunit;
using AElf.Common;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Miner.TxMemPool;
using ITxSignatureVerifier = AElf.Kernel.Types.Transaction.ITxSignatureVerifier;

namespace AElf.Kernel.Tests
{


    public sealed class TransactionResultTest : AElfKernelIntegratedTest
    {
        private readonly ITransactionResultService _transactionResultService;
        private readonly ITransactionResultManager _transactionResultManager;
    
        public TransactionResultTest()
        {
            ChainConfig.Instance.ChainId = Hash.LoadByteArray(new byte[] { 0x01, 0x02, 0x03 }).DumpBase58();
            NodeConfig.Instance.NodeAccount = Address.Generate().GetFormatted();
            _transactionResultManager = GetRequiredService<ITransactionResultManager>();
            _transactionResultService = GetRequiredService<ITransactionResultService>();
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
            Assert.True(res.Status == Status.NotExisted);
        }
    
        [Fact]
        public async Task GetTxResultInStorage()
        {
            var txId = Hash.Generate();
            var res = CreateResult(txId, Status.Mined);
            await _transactionResultManager.AddTransactionResultAsync(res);
    
            var r = await _transactionResultService.GetResultAsync(txId);
            Assert.Equal(res, r);
        }
    
    }
}
*/
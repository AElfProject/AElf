using AElf.Kernel.TxMemPool;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class TxPoolTest
    {
        private readonly IChainContextService _chainContextService;
        private readonly ITransactionManager _transactionManager;

        public TxPoolTest(IChainContextService chainContextService, ITransactionManager transactionManager)
        {
            _chainContextService = chainContextService;
            _transactionManager = transactionManager;
        }

        [Fact]
        public void StartupTest()
        {
           /* var tx = new Transaction();
            var addr1 = Hash.Generate();
            var addr2 = Hash.Generate();
            tx.From = addr1;
            tx.To = addr2;
            var res = _poolService.AddTransaction(tx).Result;
            Assert.False(res);*/
        }
        
    }
}
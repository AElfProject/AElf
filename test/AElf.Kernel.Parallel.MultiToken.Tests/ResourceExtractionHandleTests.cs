using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.SmartContract.Parallel;
using AElf.OS;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Kernel.Parallel.MultiToken.Tests
{
    public class ResourceExtractionHandleTests : ParallelMultiTokenTestBase
    {
        private IResourceExtractionService Service =>
            Application.ServiceProvider.GetRequiredService<IResourceExtractionService>();

        private IResourceExtractionService _iResourceExtractionService;
        private OSTestHelper _osTestHelper;

        public ResourceExtractionHandleTests()
        {
            _iResourceExtractionService = GetRequiredService<IResourceExtractionService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
        }
        
        [Fact]
        public async Task HandleResourceNeeded_Success()
        {
            var transactions = GenerateTransactions(5);

            var eventData = new TransactionResourcesNeededEvent()
            {
                Transactions = transactions
            };
            
            await _iResourceExtractionService.HandleTransactionResourcesNeededAsync(eventData);
        }

        [Fact]
        public async Task HandleResourceNoLongerNeeded_Success()
        {
            var transactions = GenerateTransactions(5);

            var eventData = new TransactionResourcesNeededEvent
            {
                Transactions = transactions
            };
            
            await _iResourceExtractionService.HandleTransactionResourcesNeededAsync(eventData);
            
            var eventData1 = new TransactionResourcesNoLongerNeededEvent
            {
                TransactionIds = new []{transactions.First().GetHash()}
            };
            await _iResourceExtractionService.HandleTransactionResourcesNoLongerNeededAsync(eventData1);
        }

        private IEnumerable<Transaction> GenerateTransactions(int count)
        {
            var transactions = new List<Transaction>();
            for (var i = 0; i < 5; i++)
            {
                var transaction = AsyncHelper.RunSync(()=>_osTestHelper.GenerateTransferTransaction());
                transactions.Add(transaction);
            }

            return transactions;
        }
    }
}
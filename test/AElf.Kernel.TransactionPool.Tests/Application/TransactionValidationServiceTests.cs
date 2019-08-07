using System.Threading.Tasks;
using Xunit;

namespace AElf.Kernel.TransactionPool.Application
{
    public sealed class TransactionValidationServiceTests : TransactionPoolWithChainTestBase
    {
        private readonly ITransactionValidationService _transactionValidationService;
        
        public TransactionValidationServiceTests()
        {
            var _transactionValidationService = GetRequiredService<ITransactionValidationService>();
        }

        [Fact]
        public async Task ValidateTransactionAsync_Test()
        {
            
        }
    }
}
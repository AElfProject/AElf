using System.Threading.Tasks;
using AElf.Kernel;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Election
{
    public class ElectionTests : ElectionContractTestBase
    {
        public ElectionTests()
        {
            InitializeContracts();
        }
        
        [Fact(Skip = "With issue right now, wait fix.")]
        public async Task ElectionContract_InitializeMultiTimes()
        {
            var transactionResult = (await ElectionContractStub.InitialElectionContract.SendAsync(new InitialElectionContractInput
            {
                TokenContractSystemName = Hash.Generate(),
                VoteContractSystemName = Hash.Generate()
            })).TransactionResult;
            
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Already initialized.").ShouldBeTrue();
        }
    }
}
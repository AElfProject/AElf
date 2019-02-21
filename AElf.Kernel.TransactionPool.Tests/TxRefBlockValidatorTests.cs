using System;
using System.Threading.Tasks;
using AElf.Common;
using Moq;
using Shouldly;
using Xunit;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool.RefBlockExceptions;
using Moq.Language.Flow;

namespace AElf.Kernel.TransactionPool.Tests
{
    public class TxRefBlockValidatorTests:TransactionPoolTestBase
    {
        private readonly TxRefBlockValidator _validator;
        private readonly IBlockchainService _chainService;
        private Mock<IBlockchainService> _mock;
        private IReturnsResult<IBlockchainService>  _returnsResult;
        private const int ChainId = 1234;

        public TxRefBlockValidatorTests()
        {
            _mock = new Mock<IBlockchainService>();


            _chainService = _mock.Object;
            _validator = new TxRefBlockValidator(_chainService);
        }

        [Fact]
        public void Validate_All_Status()
        {
            var transaction = FakeTransaction.Generate();
            _validator.ValidateAsync(ChainId, transaction).ShouldNotThrow();

            _returnsResult = _mock.Setup(x => x.GetChainAsync(It.IsAny<int>())).Returns(Task.FromResult<Chain>(new Chain()
            {
                BestChainHeight = 100,
            }));
            _chainService.CreateChainAsync(ChainId, new Block());

            transaction.RefBlockNumber = 102;
            _validator.ValidateAsync(ChainId, transaction).ShouldThrow<FutureRefBlockException>();

            transaction.RefBlockNumber = 30;
            _validator.ValidateAsync(ChainId, transaction).ShouldThrow<RefBlockExpiredException>();

            transaction.RefBlockNumber = 90;
            _validator.ValidateAsync(ChainId, transaction).ShouldThrow<Exception>();

            _returnsResult = _mock.Setup(x => x.GetBlockHashByHeightAsync(It.IsAny<Chain>(), It.IsAny<ulong>(), null))
                .Returns(Task.FromResult<Hash>(Hash.FromString("test")));
            _validator.ValidateAsync(ChainId, transaction).ShouldNotThrow();

        }
    }
}
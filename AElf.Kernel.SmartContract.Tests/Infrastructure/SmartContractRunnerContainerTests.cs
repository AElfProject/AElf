using System;
using System.Threading.Tasks;
using Moq;
using Org.BouncyCastle.Security;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Infrastructure
{
    public class SmartContractRunnerContainerTests : SmartContractRunnerTestBase
    {
        private readonly SmartContractRunnerContainer _smartContractRunnerContainer;

        public SmartContractRunnerContainerTests()
        {
            _smartContractRunnerContainer = GetRequiredService<SmartContractRunnerContainer>();
        }

        [Fact]
        public void Get_Runner_ReturnRunner()
        {
            var runner = _smartContractRunnerContainer.GetRunner(2);

            runner.ShouldNotBeNull();
        }

        [Fact]
        public void Get_Runner_ThrowInvalidParameterException()
        {
            Assert.Throws<InvalidParameterException>
            (
                () => _smartContractRunnerContainer.GetRunner(7)
            );
        }

        [Fact]
        public void Add_Runner_Success()
        {
            var mockSmartContractRunner = new Mock<ISmartContractRunner>();
            _smartContractRunnerContainer.AddRunner(5, mockSmartContractRunner.Object);

            var runner = _smartContractRunnerContainer.GetRunner(5);
            
            runner.ShouldNotBeNull();
        }

        [Fact]
        public void Add_Runner_ThrowInvalidParameterException()
        {
            var mockSmartContractRunner = new Mock<ISmartContractRunner>();
            _smartContractRunnerContainer.AddRunner(5, mockSmartContractRunner.Object);
            
            Assert.Throws<InvalidParameterException>
            (
                () => _smartContractRunnerContainer.AddRunner(5, mockSmartContractRunner.Object)
            );
        }

        [Fact]
        public void Update_Runner_Success()
        {
            var mockSmartContractRunner = new Mock<ISmartContractRunner>();
            _smartContractRunnerContainer.AddRunner(5, mockSmartContractRunner.Object);
            
            var mockSmartContractRunnerNew = new Mock<ISmartContractRunner>();
            _smartContractRunnerContainer.UpdateRunner(5,mockSmartContractRunnerNew.Object);

            var runner = _smartContractRunnerContainer.GetRunner(5);
            runner.ShouldNotBeSameAs(mockSmartContractRunner.Object);
            runner.ShouldBeSameAs(mockSmartContractRunnerNew.Object);
        }

        [Fact]
        public void Update_Runner_Fail()
        {
            var mockSmartContractRunner = new Mock<ISmartContractRunner>();
            _smartContractRunnerContainer.UpdateRunner(5, mockSmartContractRunner.Object);

            Assert.Throws<InvalidParameterException>
            (
                () => _smartContractRunnerContainer.GetRunner(5)
            );
        }
    }
}
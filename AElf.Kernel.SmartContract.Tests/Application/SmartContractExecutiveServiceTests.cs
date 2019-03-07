using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.SmartContract.Infrastructure;
using Google.Protobuf;
using Moq;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Application
{
    public class SmartContractExecutiveServiceTests : SmartContractRunnerTestBase
    {
        private readonly SmartContractExecutiveService _smartContractExecutiveService;

        public SmartContractExecutiveServiceTests()
        {
            _smartContractExecutiveService = GetRequiredService<SmartContractExecutiveService>();
        }

        [Fact]
        public async Task Put_ZeroRegistration_Executive_Success()
        {
            var registration = new SmartContractRegistration
            {
                Category = 2,
                Code = Hash.FromString("TestPutExecutive").ToByteString(),
                CodeHash = Hash.FromString("TestPutExecutive")
            };

            var mockExecutive = new Mock<IExecutive>();
            mockExecutive.SetupProperty(e => e.ContractHash);
            mockExecutive.Setup(e => e.SetTransactionContext(It.IsAny<TransactionContext>()))
                .Returns(mockExecutive.Object);
            mockExecutive.Setup(e => e.SetDataCache(It.IsAny<IStateCache>()));

            mockExecutive.Object.ContractHash = registration.CodeHash;

            await _smartContractExecutiveService.PutExecutiveAsync(Address.Genesis, mockExecutive.Object);
        }

        [Fact]
        public async Task Get_Executive_BySmartContractRegistration_ReturnExecutive()
        {
            var registration = new SmartContractRegistration
            {
                Category = 2,
                Code = Hash.FromString("TestGetExecutive").ToByteString(),
                CodeHash = Hash.FromString("TestGetExecutive")
            };

            var result = await _smartContractExecutiveService.GetExecutiveAsync(registration);
            result.ContractHash.ShouldBe(registration.CodeHash);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Sdk;
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
                Category = KernelConstants.DefaultRunnerCategory,
                Code = Hash.FromString("TestPutExecutive").ToByteString(),
                CodeHash = Hash.FromString("TestPutExecutive")
            };

            var mockExecutive = new Mock<IExecutive>();
            await _smartContractExecutiveService.PutExecutiveAsync(Address.Genesis, mockExecutive.Object);
        }

    }
}
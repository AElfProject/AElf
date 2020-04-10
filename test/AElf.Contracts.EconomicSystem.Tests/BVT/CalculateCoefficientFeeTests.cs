using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Contracts.EconomicSystem.Tests.BVT
{
    public partial class EconomicSystemTest
    {
        [Fact]
        public async Task UpdateCoefficient_NotChange_Test()
        {
            //invalid setting
            var tokenContractImplStub = GetTokenContractImplTester(InitialCoreDataCenterKeyPairs.Last());
            var txResult =
                await tokenContractImplStub.UpdateCoefficientsForContract.SendAsync(new UpdateCoefficientsInput())
                    .ShouldThrowAsync<Exception>();
            txResult.Message.ShouldContain("controller does not initialize");

            txResult = await tokenContractImplStub.UpdateCoefficientsForSender.SendAsync(new UpdateCoefficientsInput())
                .ShouldThrowAsync<Exception>();
            txResult.Message.ShouldContain("controller does not initialize");
        }
    }
}
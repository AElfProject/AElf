using System.Linq;
using System.Threading.Tasks;
using Acs6;
using AElf.Contracts.Economic.TestBase;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSTest
    {
        [Fact]
        internal async Task<RandomNumberOrder> AEDPoSContract_RequestRandomNumber_Test()
        {
            var randomNumberOrder =
                (await AEDPoSContractStub.RequestRandomNumber.SendAsync(new Empty())).Output;
            randomNumberOrder.TokenHash.ShouldNotBeNull();
            randomNumberOrder.BlockHeight.ShouldBeGreaterThan(
                AEDPoSContractTestConstants.InitialMinersCount.Mul(AEDPoSContractTestConstants.TinySlots));
            return randomNumberOrder;
        }
    }
}
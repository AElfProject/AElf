using System.Threading.Tasks;
using Acs2;
using AElf.Contracts.TestKit;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.MultiToken
{
    public partial class MultiTokenContractTests
    {
        [Fact]
        public async Task ACS2_GetResourceInfo_Transfer_Test()
        {
            var transaction = GenerateTokenTransaction(Accounts[0].Address, nameof(TokenContractStub.Transfer),
                new TransferInput
                {
                    Amount = 100,
                    Symbol = "ELF",
                    To = Accounts[1].Address,
                    Memo = "Test get resource"
                });
                
            var result = await Acs2BaseStub.GetResourceInfo.CallAsync(transaction);
            result.NonParallelizable.ShouldBeFalse();
            result.Paths.Count.ShouldBeGreaterThan(0);
        }
        
        [Fact]
        public async Task ACS2_GetResourceInfo_TransferFrom_Test()
        {
            var transaction = GenerateTokenTransaction(Accounts[0].Address, nameof(TokenContractStub.TransferFrom),
                new TransferFromInput
                {
                    Amount = 100,
                    Symbol = "ELF",
                    From = Accounts[1].Address,
                    To = Accounts[2].Address,
                    Memo = "Test get resource"
                });
                
            var result = await Acs2BaseStub.GetResourceInfo.CallAsync(transaction);
            result.NonParallelizable.ShouldBeFalse();
            result.Paths.Count.ShouldBeGreaterThan(0);
        }
        
        [Fact]
        public async Task ACS2_GetResourceInfo_DonateResourceToken_Test()
        {
            var transaction = GenerateTokenTransaction(Accounts[0].Address, nameof(TokenContractStub.DonateResourceToken),
                new Empty());
                
            var result = await Acs2BaseStub.GetResourceInfo.CallAsync(transaction);
            result.NonParallelizable.ShouldBeTrue();
        }
        
        [Fact]
        public async Task ACS2_GetResourceInfo_ClaimTransactionFees_Test()
        {
            var transaction = GenerateTokenTransaction(Accounts[0].Address, nameof(TokenContractStub.ClaimTransactionFees),
                new Empty());
                
            var result = await Acs2BaseStub.GetResourceInfo.CallAsync(transaction);
            result.NonParallelizable.ShouldBeTrue();
        }
        
        [Fact]
        public async Task ACS2_GetResourceInfo_UnsupportedMethod_Test()
        {
            var transaction = GenerateTokenTransaction(Accounts[0].Address, "TestMethod",
                new Empty());
                
            var result = await Acs2BaseStub.GetResourceInfo.CallAsync(transaction);
            result.ShouldBe(new ResourceInfo {NonParallelizable = true});
        }

        private Transaction GenerateTokenTransaction(Address from, string method, IMessage input)
        {
            return new Transaction
            {
                From = from,
                To = TokenContractAddress,
                MethodName = method,
                Params = ByteString.CopyFrom(input.ToByteArray())
            };           
        }
    }
}
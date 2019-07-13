using System.Threading.Tasks;
using Acs2;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel.Token;
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
        public async Task ACS2_GetResourceInfo_Transfer()
        {
            var transaction = GenerateTokenTransaction(Address.Generate(), nameof(TokenContractStub.Transfer),
                new TransferInput
                {
                    Amount = 100,
                    Symbol = "ELF",
                    To = Address.Generate(),
                    Memo = "Test get resource"
                });
                
            var result = await Acs2BaseStub.GetResourceInfo.CallAsync(transaction);
            result.NonParallelizable.ShouldBeFalse();
            result.Reources.Count.ShouldBeGreaterThan(0);
        }
        
        [Fact]
        public async Task ACS2_GetResourceInfo_TransferFrom()
        {
            var transaction = GenerateTokenTransaction(Address.Generate(), nameof(TokenContractStub.TransferFrom),
                new TransferFromInput
                {
                    Amount = 100,
                    Symbol = "ELF",
                    From = Address.Generate(),
                    To = Address.Generate(),
                    Memo = "Test get resource"
                });
                
            var result = await Acs2BaseStub.GetResourceInfo.CallAsync(transaction);
            result.NonParallelizable.ShouldBeFalse();
            result.Reources.Count.ShouldBeGreaterThan(0);
        }
        
        [Fact]
        public async Task ACS2_GetResourceInfo_DonateResourceToken()
        {
            var transaction = GenerateTokenTransaction(Address.Generate(), nameof(TokenContractStub.DonateResourceToken),
                new Empty());
                
            var result = await Acs2BaseStub.GetResourceInfo.CallAsync(transaction);
            result.NonParallelizable.ShouldBeFalse();
            result.Reources.Count.ShouldBeGreaterThan(0);
        }
        
        [Fact]
        public async Task ACS2_GetResourceInfo_ClaimTransactionFees()
        {
            var transaction = GenerateTokenTransaction(Address.Generate(), nameof(TokenContractStub.ClaimTransactionFees),
                new Empty());
                
            var result = await Acs2BaseStub.GetResourceInfo.CallAsync(transaction);
            result.NonParallelizable.ShouldBeFalse();
            result.Reources.Count.ShouldBeGreaterThan(0);
        }
        
        [Fact]
        public async Task ACS2_GetResourceInfo_UnsupportedMethod()
        {
            var transaction = GenerateTokenTransaction(Address.Generate(), "TestMethod",
                new Empty());
                
            var result = await Acs2BaseStub.GetResourceInfo.CallAsync(transaction);
            result.ShouldBe(new ResourceInfo());
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
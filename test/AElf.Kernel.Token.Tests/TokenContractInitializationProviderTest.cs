using System.Linq;
using AElf.Contracts.MultiToken;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Token.Test
{
    public class TokenContractInitializationProviderTest
    {
        [Fact]
        public void GetContractInitializationData_Without_Data_Test()
        {
            var provider = new TokenContractInitializationDataProviderWithNull();
            var contractInitializationProvider = new TokenContractInitializationProvider(provider);
            contractInitializationProvider.GetInitializeMethodList(null).Count.ShouldBe(0);
        }

        [Fact]
        public void GetContractInitializationData_With_Primary_Token_Test()
        {
            var issueCount = 4;
            var provider = new TokenContractInitializationDataProviderWithPrimaryToken(issueCount);
            var contractInitializationProvider = new TokenContractInitializationProvider(provider);
            var methodCallList = contractInitializationProvider.GetInitializeMethodList(null);
            methodCallList.Count(x => x.MethodName == nameof(TokenContractContainer.TokenContractStub.Create))
                .ShouldBe(4);
            methodCallList
                .Count(x => x.MethodName == nameof(TokenContractContainer.TokenContractStub.InitialCoefficients))
                .ShouldBe(1);
            methodCallList
                .Count(x => x.MethodName == nameof(TokenContractContainer.TokenContractStub.SetPrimaryTokenSymbol))
                .ShouldBe(1);
            methodCallList.Count(x =>
                    x.MethodName == nameof(TokenContractContainer.TokenContractStub.InitializeAuthorizedController))
                .ShouldBe(1);
            methodCallList.Count(x =>
                    x.MethodName == nameof(TokenContractContainer.TokenContractStub.InitializeFromParentChain))
                .ShouldBe(1);
            methodCallList.Count(x =>
                    x.MethodName == nameof(TokenContractContainer.TokenContractStub.Issue))
                .ShouldBe(issueCount);
            methodCallList.Count.ShouldBe(8 + issueCount);
        }

        [Fact]
        public void GetContractInitializationData_Without_Primary_Token_Test()
        {
            var provider = new TokenContractInitializationDataProviderWithoutPrimaryToken();
            var contractInitializationProvider = new TokenContractInitializationProvider(provider);
            var methodCallList = contractInitializationProvider.GetInitializeMethodList(null);
            methodCallList.Count(x => x.MethodName == nameof(TokenContractContainer.TokenContractStub.Create))
                .ShouldBe(3);
            methodCallList
                .Count(x => x.MethodName == nameof(TokenContractContainer.TokenContractStub.InitialCoefficients))
                .ShouldBe(1);
            methodCallList
                .Count(x => x.MethodName == nameof(TokenContractContainer.TokenContractStub.SetPrimaryTokenSymbol))
                .ShouldBe(1);
            methodCallList.Count(x =>
                    x.MethodName == nameof(TokenContractContainer.TokenContractStub.InitializeAuthorizedController))
                .ShouldBe(1);
            methodCallList.Count(x =>
                    x.MethodName == nameof(TokenContractContainer.TokenContractStub.InitializeFromParentChain))
                .ShouldBe(1);
            methodCallList.Count.ShouldBe(7);
        }
    }
}
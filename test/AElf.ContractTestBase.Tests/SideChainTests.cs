using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.CrossChain;
using AElf.Contracts.MultiToken;
using AElf.CrossChain;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.ContractTestBase.Tests
{
    public class SideChainTests : SideChainTestBase
    {
        private readonly IBlockchainService _blockchainService;
        protected readonly IContractInitializationProvider _tokenContractInitializationProvider;
        protected readonly IPrimaryTokenSymbolService _primaryTokenSymbolService;

        public SideChainTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _primaryTokenSymbolService = GetRequiredService<IPrimaryTokenSymbolService>();
            var IContractInitializationProviderList =
                GetRequiredService<IEnumerable<IContractInitializationProvider>>();
            _tokenContractInitializationProvider =
                IContractInitializationProviderList.Single(x =>
                    x.GetType() == typeof(TokenContractInitializationProvider));
        }

        [Fact]
        public async Task Test()
        {
            var preBlockHeader = await _blockchainService.GetBestChainLastBlockHeaderAsync();
            var chainContext = new ChainContext
            {
                BlockHash = preBlockHeader.GetHash(),
                BlockHeight = preBlockHeader.Height
            };
            var contractMapping = await ContractAddressService.GetSystemContractNameToAddressMappingAsync(chainContext);

            var crossChainStub = GetTester<CrossChainContractContainer.CrossChainContractStub>(
                contractMapping[CrossChainSmartContractAddressNameProvider.Name], Accounts[0].KeyPair);
            var parentChainId = await crossChainStub.GetParentChainId.CallAsync(new Empty());
            ChainHelper.ConvertChainIdToBase58(parentChainId.Value).ShouldBe("AELF");
        }
        
        [Fact]
        public async Task SideChain_GetPrimaryTokenSymbol_Test()
        {
            var primaryTokenSymbol = await _primaryTokenSymbolService.GetPrimaryTokenSymbol();
            primaryTokenSymbol.ShouldBe("ELF");
        }
        
        [Fact]
        public void SideChain_Token_GetInitializeMethodList_Test()
        {
            var methodCallList = _tokenContractInitializationProvider.GetInitializeMethodList(new byte[] { });
            
            //should be same as SideChainTokenContractInitializationDataProvider
            methodCallList.Exists(x => x.MethodName == nameof(TokenContractContainer.TokenContractStub.Create))
                .ShouldBeTrue();
            methodCallList
                .Exists(x => x.MethodName == nameof(TokenContractContainer.TokenContractStub.InitialCoefficients))
                .ShouldBeTrue();
            methodCallList
                .Exists(x => x.MethodName == nameof(TokenContractContainer.TokenContractStub.SetPrimaryTokenSymbol))
                .ShouldBeTrue();
            methodCallList.Exists(x =>
                    x.MethodName == nameof(TokenContractContainer.TokenContractStub.InitializeAuthorizedController))
                .ShouldBeTrue();
            methodCallList.Exists(x =>
                    x.MethodName == nameof(TokenContractContainer.TokenContractStub.InitializeFromParentChain))
                .ShouldBeTrue();
        }
    }
}
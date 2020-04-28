using System.Collections.Generic;
using System.Linq;
using AElf.CrossChain.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Volo.Abp.Threading;

namespace AElf.ContractTestBase
{
    public class SideChainTokenContractInitializationDataProvider : ITokenContractInitializationDataProvider
    {
        private readonly ISideChainInitializationDataProvider _sideChainInitializationDataProvider;

        public SideChainTokenContractInitializationDataProvider(
            ISideChainInitializationDataProvider sideChainInitializationDataProvider)
        {
            _sideChainInitializationDataProvider = sideChainInitializationDataProvider;
        }

        public TokenContractInitializationData GetContractInitializationData()
        {
            var sideChainInitializationData =
                AsyncHelper.RunSync(_sideChainInitializationDataProvider.GetChainInitializationDataAsync);

            return new TokenContractInitializationData
            {
                Creator = sideChainInitializationData.Creator,
                ResourceAmount = sideChainInitializationData.ResourceTokenInfo.InitialResourceAmount.ToDictionary(
                    kv => kv.Key.ToUpper(),
                    kv => kv.Value),
                NativeTokenInfoData = sideChainInitializationData.NativeTokenInfoData,
                PrimaryTokenInfoData = sideChainInitializationData.ChainPrimaryTokenInfo?.ChainPrimaryTokenData,
                ResourceTokenListData = sideChainInitializationData.ResourceTokenInfo.ResourceTokenListData,
                TokenInitialIssueList =
                    sideChainInitializationData.ChainPrimaryTokenInfo?.SideChainTokenInitialIssueList
                        .Select(t => new TokenInitialIssue {Address = t.Address, Amount = t.Amount}).ToList(),
                RegisteredOtherTokenContractAddresses = new Dictionary<int, Address>
                {
                    {
                        _sideChainInitializationDataProvider.ParentChainId,
                        sideChainInitializationData.ParentChainTokenContractAddress
                    }
                }
            };
        }
    }
}
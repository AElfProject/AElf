using System.Linq;
using System.Threading.Tasks;
using AElf.Standards.ACS7;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.ContractTestBase.ContractTestKit;
using AElf.CrossChain.Application;
using AElf.Kernel;
using Google.Protobuf;
using Microsoft.Extensions.Options;

namespace AElf.Contracts.MultiToken
{
    public class SideChainInitializationDataProvider : ISideChainInitializationDataProvider
    {
        public int ParentChainId => _chainInitializationOptions.ParentChainId;

        private readonly ChainInitializationOptions _chainInitializationOptions;

        public SideChainInitializationDataProvider(IOptionsSnapshot<ChainInitializationOptions> chainInitializationOptions)
        {
            _chainInitializationOptions = chainInitializationOptions.Value;
        }

        public Task<ChainInitializationData> GetChainInitializationDataAsync()
        {
            var address = SampleAccount.Accounts.First().Address;
            // Default Initialization Data
            return Task.FromResult(new ChainInitializationData
            {
                Creator = address,
                ChainId = _chainInitializationOptions.ChainId,
                ChainCreatorPrivilegePreserved = true,
                ChainInitializationConsensusInfo = new ChainInitializationConsensusInfo
                {
                    InitialConsensusData = new MinerListWithRoundNumber
                    {
                        MinerList = new MinerList()
                        {
                            Pubkeys =
                            {
                                SampleAccount.Accounts.Take(3)
                                    .Select(a => ByteString.CopyFrom(a.KeyPair.PublicKey))
                            }
                        }
                    }.ToByteString()
                },
                CreationHeightOnParentChain = _chainInitializationOptions.CreationHeightOnParentChain,
                CreationTimestamp = TimestampHelper.GetUtcNow(),
                NativeTokenInfoData = new TokenInfo
                {
                    Symbol = "ELF",
                    TokenName = "ELF",
                    Decimals = 8,
                    TotalSupply = 100_000_000_000_000_000,
                    Issuer = address,
                    IssueChainId = ParentChainId,
                }.ToByteString(),
                ParentChainTokenContractAddress = _chainInitializationOptions.RegisterParentChainTokenContractAddress
                    ? _chainInitializationOptions.ParentChainTokenContractAddress
                    : null,
                ResourceTokenInfo = new ResourceTokenInfo(),
                ChainPrimaryTokenInfo = new ChainPrimaryTokenInfo
                {
                    ChainPrimaryTokenData = new TokenInfo
                    {
                        Decimals = 2,
                        IsBurnable = true,
                        Issuer = address,
                        TotalSupply = 1_000_000_000,
                        Symbol = _chainInitializationOptions.Symbol,
                        TokenName = "TEST",
                        IssueChainId = _chainInitializationOptions.ChainId
                    }.ToByteString()
                }
            });
        }

        
    }
}
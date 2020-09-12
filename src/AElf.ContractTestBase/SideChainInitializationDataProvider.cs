using System.Linq;
using System.Threading.Tasks;
using AElf.Standards.ACS7;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.ContractTestBase.ContractTestKit;
using AElf.CrossChain;
using AElf.CrossChain.Application;
using AElf.Kernel;
using Google.Protobuf;
using Microsoft.Extensions.Options;

namespace AElf.ContractTestBase
{
    public class SideChainInitializationDataProvider : ISideChainInitializationDataProvider
    {
        public SideChainInitializationDataProvider(IOptionsSnapshot<CrossChainConfigOptions> crossChainConfigOptions)
        {
            ParentChainId = ChainHelper.ConvertBase58ToChainId(crossChainConfigOptions.Value.ParentChainId);
        }

        public async Task<ChainInitializationData> GetChainInitializationDataAsync()
        {
            // Default Initialization Data
            return new ChainInitializationData
            {
                Creator = SampleAccount.Accounts.First().Address,
                ChainId = ChainHelper.GetChainId(1),
                ChainCreatorPrivilegePreserved = false,
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
                CreationHeightOnParentChain = 100,
                CreationTimestamp = TimestampHelper.GetUtcNow(),
                NativeTokenInfoData = new TokenInfo
                {
                    Symbol = "ELF",
                    TokenName = "ELF",
                    Decimals = 8,
                    TotalSupply = 100_000_000_000_000_000,
                    Issuer = SampleAccount.Accounts.First().Address,
                    IssueChainId = ParentChainId,
                }.ToByteString(),
                ParentChainTokenContractAddress = SampleAddress.AddressList.Last(),
                ResourceTokenInfo = new ResourceTokenInfo()
            };
        }

        public int ParentChainId { get; }
    }
}
using System.Linq;
using System.Threading.Tasks;
using Acs7;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKit;
using AElf.CrossChain;
using AElf.CrossChain.Application;
using AElf.Kernel;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

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
                Creator = SampleAddress.AddressList.First(),
                ChainId = ChainHelper.GetChainId(1),
                ChainCreatorPrivilegePreserved = false,
                ChainInitializationConsensusInfo = new ChainInitializationConsensusInfo
                {
                    InitialMinerListData = new MinerListWithRoundNumber
                    {
                        MinerList = new MinerList()
                        {
                            Pubkeys =
                            {
                                SampleECKeyPairs.KeyPairs.Take(3)
                                    .Select(keyPair => ByteString.CopyFrom(keyPair.PublicKey))
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
                    Issuer = SampleAddress.AddressList.First(),
                    IssueChainId = ParentChainId,
                }.ToByteString(),
                ParentChainTokenContractAddress = SampleAddress.AddressList.Last(),
                ResourceTokenInfo = new ResourceTokenInfo()
            };
        }

        public int ParentChainId { get; }
    }
}
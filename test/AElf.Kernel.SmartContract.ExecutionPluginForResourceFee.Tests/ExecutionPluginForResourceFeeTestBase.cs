using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.ContractTestKit;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core.Extension;
using AElf.EconomicSystem;
using AElf.Kernel.Proposal;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using InitializeInput = AElf.Contracts.TokenConverter.InitializeInput;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee.Tests
{
    public class ExecutionPluginForResourceFeeTestBase : ContractTestBase<ExecutionPluginForResourceFeeTestModule>
    {
        internal const long StoUnitPrice = 1_00000000;

        //init connectors
        internal Connector ElfConnector = new Connector
        {
            Symbol = "ELF",
            VirtualBalance = 100_000_00000000,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = true
        };

        internal Connector ReadConnector = new Connector
        {
            Symbol = "READ",
            VirtualBalance = 100_000_00000000,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = true,// For testing
            RelatedSymbol = "NTREAD"
        };
        internal Connector NativeToReadConnector = new Connector
        {
            Symbol = "NTREAD",
            VirtualBalance = 100_000_00000000,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = true,
            RelatedSymbol = "READ"
        };
        
        internal Connector StoConnector = new Connector
        {
            Symbol = "STORAGE",
            VirtualBalance = 100_000_00000000,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = true,// For testing
            RelatedSymbol = "NTSTORAGE"
        };
        internal Connector NativeToStoConnector = new Connector
        {
            Symbol = "NTSTORAGE",
            VirtualBalance = 100_000_00000000,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = true,
            RelatedSymbol = "STORAGE"
        };
        internal Connector NetConnector = new Connector
        {
            Symbol = "TRAFFIC",
            VirtualBalance = 100_000_00000000,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = true,// For testing
            RelatedSymbol = "NTTRAFFIC"
        };
        internal Connector NativeToNetConnector = new Connector
        {
            Symbol = "NTTRAFFIC",
            VirtualBalance = 100_000_00000000,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = true,// For testing
            RelatedSymbol = "TRAFFIC"
        };
        internal Connector WriteConnector = new Connector
        {
            Symbol = "WRITE",
            VirtualBalance = 100_000_00000000,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = true,// For testing
            RelatedSymbol = "NTWRITE"
        };
        internal Connector NativeToWriteConnector = new Connector
        {
            Symbol = "NTWRITE",
            VirtualBalance = 100_000_00000000,
            Weight = "0.5",
            IsPurchaseEnabled = true,
            IsVirtualBalanceEnabled = true,// For testing
            RelatedSymbol = "WRITE"
        };

        internal Address TestContractAddress { get; set; }
        internal Address TokenContractAddress { get; set; }
        internal Address TokenConverterAddress { get; set; }
        internal Address TreasuryContractAddress { get; set; }
        internal TestContract.ContractContainer.ContractStub TestContractStub { get; set; }
        internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
        internal TokenConverterContractContainer.TokenConverterContractStub TokenConverterContractStub { get; set; }
        internal TreasuryContractContainer.TreasuryContractStub TreasuryContractStub { get; set; }
        internal ParliamentContractContainer.ParliamentContractStub ParliamentContractStub { get; set; }
        internal AEDPoSContractContainer.AEDPoSContractStub AEDPoSContractStub { get; set; }

        internal ECKeyPair DefaultSenderKeyPair => Accounts[0].KeyPair;
        protected List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
            Accounts.Take(1).Select(a => a.KeyPair).ToList();
        internal ECKeyPair OtherTester => Accounts[1].KeyPair;
        internal Address DefaultSender => Accounts[0].Address;
        protected Address FeeReceiverAddress => Accounts[10].Address;
        protected Address ManagerAddress => Accounts[11].Address;

        protected async Task InitializeContracts()
        {
            await DeployContractsAsync();
            await InitializeTokenAsync();
            await InitializeParliament();
            await InitializeAElfConsensus();
            await InitializeTreasuryContractAsync();
            await InitializeTokenConverterAsync();
        }
        
        private async Task DeployContractsAsync()
        {
            const int category = KernelConstants.CodeCoverageRunnerCategory;
            // Profit contract
            {
                var code = Codes.Single(kv => kv.Key.Contains("Profit")).Value;
                await DeploySystemSmartContract(category, code,
                    ProfitSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
            }

            // Token contract
            {
                var code = Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
                TokenContractAddress = await DeploySystemSmartContract(category, code,
                    TokenSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
                TokenContractStub =
                    GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultSenderKeyPair);
            }

            // Token converter
            {
                var code = Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value;
                TokenConverterAddress = await DeploySystemSmartContract(category, code,
                    TokenConverterSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
                TokenConverterContractStub =
                    GetTester<TokenConverterContractContainer.TokenConverterContractStub>(TokenConverterAddress,
                        DefaultSenderKeyPair);
            }
            
            // Treasury
            {
                var code = Codes.Single(kv => kv.Key.Contains("Treasury")).Value;
                TreasuryContractAddress = await DeploySystemSmartContract(category, code,
                    TreasurySmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
                TreasuryContractStub =
                    GetTester<TreasuryContractContainer.TreasuryContractStub>(TreasuryContractAddress,
                        DefaultSenderKeyPair);
            }

            // Test contract
            {
                var code = Codes.Single(kv => kv.Key.Contains("TestContract")).Value;
                TestContractAddress = await DeployContractAsync(category, code, HashHelper.ComputeFrom("TestContract"),
                    DefaultSenderKeyPair);
                TestContractStub =
                    GetTester<ExecutionPluginForResourceFee.Tests.TestContract.ContractContainer.ContractStub>(TestContractAddress, DefaultSenderKeyPair);
            }
            
            // Parliament
            {
                var code = Codes.Single(kv => kv.Key.Contains("Parliament")).Value;
                var parliamentContractAddress = await DeploySystemSmartContract(category, code,
                    ParliamentSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
                ParliamentContractStub =
                    GetTester<ParliamentContractContainer.ParliamentContractStub>(parliamentContractAddress,
                        DefaultSenderKeyPair);
            }
            
            //Consensus
            {
                 var code = Codes.Single(kv => kv.Key.Contains("AEDPoS")).Value;
                 var consensusContractAddress = await DeploySystemSmartContract(category, code,
                     HashHelper.ComputeFrom("AElf.ContractNames.Consensus"), DefaultSenderKeyPair);
                 AEDPoSContractStub =
                     GetTester<AEDPoSContractContainer.AEDPoSContractStub>(consensusContractAddress,
                         DefaultSenderKeyPair);
            }
        }

        private async Task InitializeTokenAsync()
        {
            const long totalSupply = 1_000_000_000_00000000;
            const long issueAmount = 1_000_000_00000000;
            const long issueAmountToConverter = 100_000_000_00000000;
            //init elf token
            {
                var createResult = await TokenContractStub.Create.SendAsync(new CreateInput
                {
                    Symbol = "ELF",
                    Decimals = 8,
                    IsBurnable = true,
                    TokenName = "elf token",
                    TotalSupply = totalSupply,
                    Issuer = DefaultSender,
                    LockWhiteList = {TreasuryContractAddress, TokenConverterAddress}
                });

                createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                {
                    var issueResult = await TokenContractStub.Issue.SendAsync(new IssueInput()
                    {
                        Symbol = "ELF",
                        Amount = issueAmount,
                        To = DefaultSender,
                    });
                    issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                }
                {
                    var issueResult = await TokenContractStub.Issue.SendAsync(new IssueInput()
                    {
                        Symbol = "ELF",
                        Amount = issueAmountToConverter,
                        To = TokenConverterAddress,
                        Memo = "Set for elf token converter."
                    });
                    issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                }
            }

            //init resource token - CPU
            {
                var createResult = await TokenContractStub.Create.SendAsync(new CreateInput
                {
                    Symbol = "READ",
                    Decimals = 2,
                    IsBurnable = true,
                    TokenName = "read token",
                    TotalSupply = totalSupply,
                    Issuer = DefaultSender,
                    LockWhiteList = {TreasuryContractAddress, TokenConverterAddress}
                });

                createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var issueResult = await TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = "READ",
                    Amount = issueAmount,
                    To = TokenConverterAddress,
                    Memo = "Set for read token converter."
                });
                issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
            
            //init resource token - STO
            {
                var createResult = await TokenContractStub.Create.SendAsync(new CreateInput
                {
                    Symbol = "STORAGE",
                    Decimals = 2,
                    IsBurnable = true,
                    TokenName = "sto token",
                    TotalSupply = totalSupply,
                    Issuer = DefaultSender,
                    LockWhiteList = {TreasuryContractAddress, TokenConverterAddress}
                });

                createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var issueResult = await TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = "STORAGE",
                    Amount = issueAmount,
                    To = TokenConverterAddress,
                    Memo = "Set for sto token converter."
                });
                issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
            
            //init resource token - NET
            {
                var createResult = await TokenContractStub.Create.SendAsync(new CreateInput
                {
                    Symbol = "TRAFFIC",
                    Decimals = 2,
                    IsBurnable = true,
                    TokenName = "net token",
                    TotalSupply = totalSupply,
                    Issuer = DefaultSender,
                    LockWhiteList = {TreasuryContractAddress, TokenConverterAddress}
                });

                createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var issueResult = await TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = "TRAFFIC",
                    Amount = issueAmount,
                    To = TokenConverterAddress,
                    Memo = "Set for net token converter."
                });
                issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
            //init resource token - WRITE
            {
                var createResult = await TokenContractStub.Create.SendAsync(new CreateInput
                {
                    Symbol = "WRITE",
                    Decimals = 2,
                    IsBurnable = true,
                    TokenName = "WRITE token",
                    TotalSupply = totalSupply,
                    Issuer = DefaultSender,
                    LockWhiteList = {TreasuryContractAddress, TokenConverterAddress}
                });

                createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var issueResult = await TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = "WRITE",
                    Amount = issueAmount,
                    To = TokenConverterAddress,
                    Memo = "Set for WRITE token converter."
                });
                issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
        }

        private async Task InitializeTokenConverterAsync()
        {
            var input = new InitializeInput
            {
                BaseTokenSymbol = "ELF",
                FeeRate = "0.005",
                Connectors =
                {
                    ElfConnector, ReadConnector, StoConnector, NetConnector, NativeToReadConnector, NativeToStoConnector,
                    NativeToNetConnector, WriteConnector, NativeToWriteConnector
                }
            };

            var initializeResult = await TokenConverterContractStub.Initialize.SendAsync(input);
            initializeResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        private async Task InitializeTreasuryContractAsync()
        {
            {
                var result =
                    await TreasuryContractStub.InitialTreasuryContract.SendAsync(new Empty());
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
            {
                var result =
                    await TreasuryContractStub.InitialMiningRewardProfitItem.SendAsync(
                        new Empty());
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
        }

        private async Task InitializeParliament()
        {
            await ParliamentContractStub.Initialize.SendAsync(new Contracts.Parliament.InitializeInput());
        }
        
        private async Task InitializeAElfConsensus()
        {
            {
                await AEDPoSContractStub.InitialAElfConsensusContract.SendAsync(
                    new InitialAElfConsensusContractInput
                    {
                        PeriodSeconds = 604800L,
                        MinerIncreaseInterval = 31536000
                    });
            }
            {
                await AEDPoSContractStub.FirstRound.SendAsync(
                    GenerateFirstRoundOfNewTerm(
                        new MinerList
                            {Pubkeys = {InitialCoreDataCenterKeyPairs.Select(p => ByteString.CopyFrom(p.PublicKey))}},
                        4000, TimestampHelper.GetUtcNow()));
            }
        }
        private Round GenerateFirstRoundOfNewTerm(MinerList minerList, int miningInterval,
            Timestamp currentBlockTime, long currentRoundNumber = 0, long currentTermNumber = 0)
        {
            var sortedMiners = minerList.Pubkeys.Select(x => x.ToHex()).ToList();
            var round = new Round();
        
            for (var i = 0; i < sortedMiners.Count; i++)
            {
                var minerInRound = new MinerInRound();
        
                // The third miner will be the extra block producer of first round of each term.
                if (i == 0)
                {
                    minerInRound.IsExtraBlockProducer = true;
                }
        
                minerInRound.Pubkey = sortedMiners[i];
                minerInRound.Order = i + 1;
                minerInRound.ExpectedMiningTime = currentBlockTime.AddMilliseconds(i * miningInterval + miningInterval);
                // Should be careful during validation.
                minerInRound.PreviousInValue = Hash.Empty;
                round.RealTimeMinersInformation.Add(sortedMiners[i], minerInRound);
            }
        
            round.RoundNumber = currentRoundNumber + 1;
            round.TermNumber = currentTermNumber + 1;
            round.IsMinerListJustChanged = true;
            round.ExtraBlockProducerOfPreviousRound = sortedMiners[0];
        
            return round;
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Contracts.TestContract.BasicFunctionWithParallel;
using AElf.Contracts.TestContract.BasicSecurity;
using AElf.Contracts.TestContract.BasicUpdate;
using AElf.Contracts.TestContract.BigIntValue;
using AElf.Contracts.TestContract.TransactionFees;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core.Extension;
using AElf.EconomicSystem;
using AElf.Kernel;
using AElf.Kernel.CodeCheck.Infrastructure;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.ExecutionPluginForResourceFee.Tests.TestContract;
using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.Threading;
using Xunit;
using InitialBasicContractInput = AElf.Contracts.TestContract.BasicFunction.InitialBasicContractInput;
using InitializeInput = AElf.Contracts.Parliament.InitializeInput;
using SmartContractConstants = AElf.Sdk.CSharp.SmartContractConstants;

namespace AElf.Contract.TestContract;

public class TestContractTestBase : ContractTestBase<TestContractAElfModule>
{
    private const string ContractPatchedDllDir = "../../../../patched/";

    protected readonly Hash TestBasicFunctionContractSystemName =
        HashHelper.ComputeFrom("AElf.ContractNames.TestContract.BasicFunction");

    protected readonly Hash TestBasicSecurityContractSystemName =
        HashHelper.ComputeFrom("AElf.ContractNames.TestContract.BasicSecurity");

    protected readonly Hash TestBigIntValueContractSystemName =
        HashHelper.ComputeFrom("AElf.ContractNames.TestContract.BigIntValue");

    public TestContractTestBase()
    {
        PatchedCodes = GetPatchedCodes(ContractPatchedDllDir);
    }

    protected ECKeyPair DefaultSenderKeyPair => Accounts[0].KeyPair;
    protected Address DefaultSender => Accounts[0].Address;
    protected new Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();
    protected Address BasicFunctionContractAddress { get; set; }
    protected Address BasicSecurityContractAddress { get; set; }
    protected Address BigIntValueContractAddress { get; set; }

    internal ACS0Container.ACS0Stub BasicContractZeroStub { get; set; }

    internal BasicFunctionContractContainer.BasicFunctionContractStub TestBasicFunctionContractStub { get; set; }

    internal BasicSecurityContractContainer.BasicSecurityContractStub TestBasicSecurityContractStub { get; set; }

    internal BigIntValueContractContainer.BigIntValueContractStub BigIntValueContractStub { get; set; }

    private IReadOnlyDictionary<string, byte[]> PatchedCodes { get; }

    internal ACS0Container.ACS0Stub GetContractZeroTester(ECKeyPair keyPair)
    {
        return GetTester<ACS0Container.ACS0Stub>(ContractZeroAddress, keyPair);
    }

    internal BasicFunctionContractContainer.BasicFunctionContractStub GetTestBasicFunctionContractStub(
        ECKeyPair keyPair)
    {
        return GetTester<BasicFunctionContractContainer.BasicFunctionContractStub>(BasicFunctionContractAddress,
            keyPair);
    }

    internal BasicUpdateContractContainer.BasicUpdateContractStub GetTestBasicUpdateContractStub(ECKeyPair keyPair)
    {
        return GetTester<BasicUpdateContractContainer.BasicUpdateContractStub>(BasicFunctionContractAddress,
            keyPair);
    }

    internal BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
        GetTestBasicFunctionWithParallelContractStub(ECKeyPair keyPair)
    {
        return GetTester<BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub>(
            BasicFunctionContractAddress,
            keyPair);
    }

    internal BasicSecurityContractContainer.BasicSecurityContractStub GetTestBasicSecurityContractStub(
        ECKeyPair keyPair)
    {
        return GetTester<BasicSecurityContractContainer.BasicSecurityContractStub>(BasicSecurityContractAddress,
            keyPair);
    }

    internal BigIntValueContractContainer.BigIntValueContractStub GetBigIntValueContractStub(
        ECKeyPair keyPair)
    {
        return GetTester<BigIntValueContractContainer.BigIntValueContractStub>(BigIntValueContractAddress,
            keyPair);
    }

    protected void InitializeTestContracts()
    {
        BasicContractZeroStub = GetContractZeroTester(DefaultSenderKeyPair);

        //deploy test contract1
        BasicFunctionContractAddress = AsyncHelper.RunSync(async () =>
            await DeployContractAsync(
                KernelConstants.CodeCoverageRunnerCategory,
                Codes.Single(kv => kv.Key.EndsWith("BasicFunction")).Value,
                TestBasicFunctionContractSystemName,
                DefaultSenderKeyPair));
        TestBasicFunctionContractStub = GetTestBasicFunctionContractStub(DefaultSenderKeyPair);
        AsyncHelper.RunSync(async () => await InitialBasicFunctionContract());

        //deploy test contract2
        BasicSecurityContractAddress = AsyncHelper.RunSync(async () =>
            await DeployContractAsync(
                KernelConstants.CodeCoverageRunnerCategory,
                Codes.Single(kv => kv.Key.EndsWith("BasicSecurity")).Value,
                TestBasicSecurityContractSystemName,
                DefaultSenderKeyPair));
        TestBasicSecurityContractStub = GetTestBasicSecurityContractStub(DefaultSenderKeyPair);
        AsyncHelper.RunSync(async () => await InitializeSecurityContract());

        //deploy test contract3
        BigIntValueContractAddress = AsyncHelper.RunSync(async () =>
            await DeployContractAsync(
                KernelConstants.CodeCoverageRunnerCategory,
                Codes.Single(kv => kv.Key.EndsWith("BigIntValue")).Value,
                TestBigIntValueContractSystemName,
                DefaultSenderKeyPair));
        BigIntValueContractStub = GetBigIntValueContractStub(DefaultSenderKeyPair);
    }

    protected void InitializePatchedContracts()
    {
        BasicContractZeroStub = GetContractZeroTester(DefaultSenderKeyPair);

        //deploy test contract1
        var basicFunctionPatchedCode = PatchedCodes.Single(kv => kv.Key.EndsWith("BasicFunction")).Value;
        CheckCode(basicFunctionPatchedCode);
        BasicFunctionContractAddress = AsyncHelper.RunSync(async () =>
            await DeployContractAsync(
                KernelConstants.CodeCoverageRunnerCategory,
                basicFunctionPatchedCode,
                TestBasicFunctionContractSystemName,
                DefaultSenderKeyPair));
        TestBasicFunctionContractStub = GetTestBasicFunctionContractStub(DefaultSenderKeyPair);
        AsyncHelper.RunSync(async () => await InitialBasicFunctionContract());

        //deploy test contract2
        var basicSecurityContractCode = PatchedCodes.Single(kv => kv.Key.EndsWith("BasicSecurity")).Value;
        BasicSecurityContractAddress = AsyncHelper.RunSync(async () =>
            await DeployContractAsync(
                KernelConstants.CodeCoverageRunnerCategory,
                basicSecurityContractCode,
                TestBasicSecurityContractSystemName,
                DefaultSenderKeyPair));
        TestBasicSecurityContractStub = GetTestBasicSecurityContractStub(DefaultSenderKeyPair);
        AsyncHelper.RunSync(async () => await InitializeSecurityContract());

        CheckCode(basicSecurityContractCode);
    }


    protected void CheckCode(byte[] code)
    {
        var auditor = GetRequiredService<IContractAuditor>();
        auditor.Audit(code, new RequiredAcs { AcsList = new List<string>() }, false);
    }

    private async Task InitialBasicFunctionContract()
    {
        await TestBasicFunctionContractStub.InitialBasicFunctionContract.SendAsync(
            new InitialBasicContractInput
            {
                ContractName = "Test Contract1",
                MinValue = 10L,
                MaxValue = 1000L,
                MortgageValue = 1000_000_000L,
                Manager = Accounts[1].Address
            });
    }

    private async Task InitializeSecurityContract()
    {
        await TestBasicSecurityContractStub.InitialBasicSecurityContract.SendAsync(BasicFunctionContractAddress);
    }
}

public class TestFeesContractTestBase : ContractTestBase<TestFeesContractAElfModule>
{
    protected const long Supply = 1_000_000_00000000L;
    protected const long VirtualElfSupplyToken = 1_000_000_00000000L;
    protected const long ResourceTokenTotalSupply = 1_000_000_000_00000000;
    protected const long VirtualResourceToken = 100_000;

    internal static readonly List<string> ResourceTokenSymbols = new() { "WRITE", "READ", "TRAFFIC", "STORAGE" };

    internal static readonly List<string> NativTokenToSourceSymbols =
        new() { "NTWRITE", "NTREAD", "NTTRAFFIC", "NTSTORAGE" };

    protected ECKeyPair DefaultSenderKeyPair => Accounts[0].KeyPair;
    protected Address DefaultSender => Accounts[0].Address;

    protected List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
        Accounts.Take(1).Select(a => a.KeyPair).ToList();

    protected Address OtherTester => Accounts[1].Address;
    protected Address ManagerTester => Accounts[10].Address;
    protected new Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();
    protected Address Acs8ContractAddress { get; set; }
    protected Address TokenContractAddress { get; set; }
    protected Address TokenConverterContractAddress { get; set; }
    protected Address TransactionFeesContractAddress { get; set; }
    protected Address TreasuryContractAddress { get; set; }
    internal ACS0Container.ACS0Stub BasicContractZeroStub { get; set; }
    internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
    internal TokenConverterContractContainer.TokenConverterContractStub TokenConverterContractStub { get; set; }
    internal TreasuryContractContainer.TreasuryContractStub TreasuryContractStub { get; set; }
    internal ParliamentContractContainer.ParliamentContractStub ParliamentContractStub { get; set; }

    internal ContractContainer.ContractStub
        Acs8ContractStub { get; set; }

    internal TransactionFeesContractContainer.TransactionFeesContractStub TransactionFeesContractStub { get; set; }
    internal AEDPoSContractContainer.AEDPoSContractStub AEDPoSContractStub { get; set; }

    protected async Task DeployTestContracts()
    {
        BasicContractZeroStub = GetTester<ACS0Container.ACS0Stub>(ContractZeroAddress, DefaultSenderKeyPair);

        var code = Codes.Single(kv => kv.Key.Contains("Profit")).Value;
        await DeploySystemSmartContract(KernelConstants.CodeCoverageRunnerCategory, code,
            ProfitSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);

        TokenContractAddress = await DeploySystemSmartContract(
            KernelConstants.CodeCoverageRunnerCategory,
            Codes.Single(kv => kv.Key.EndsWith("MultiToken")).Value,
            SmartContractConstants.TokenContractSystemHashName,
            DefaultSenderKeyPair
        );
        TokenContractStub =
            GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultSenderKeyPair);

        TokenConverterContractAddress = await DeploySystemSmartContract(
            KernelConstants.CodeCoverageRunnerCategory,
            Codes.Single(kv => kv.Key.EndsWith("TokenConverter")).Value,
            TokenConverterSmartContractAddressNameProvider.Name,
            DefaultSenderKeyPair
        );

        TreasuryContractAddress = await DeploySystemSmartContract(
            KernelConstants.CodeCoverageRunnerCategory,
            Codes.Single(kv => kv.Key.EndsWith("Treasury")).Value,
            SmartContractConstants.TreasuryContractSystemHashName,
            DefaultSenderKeyPair);
        TreasuryContractStub =
            GetTester<TreasuryContractContainer.TreasuryContractStub>(TreasuryContractAddress,
                DefaultSenderKeyPair);

        TokenConverterContractStub =
            GetTester<TokenConverterContractContainer.TokenConverterContractStub>(TokenConverterContractAddress,
                DefaultSenderKeyPair);

        var parliamentAddress = await DeploySystemSmartContract(
            KernelConstants.CodeCoverageRunnerCategory,
            Codes.Single(kv => kv.Key.EndsWith("Parliament")).Value,
            SmartContractConstants.ParliamentContractSystemHashName,
            DefaultSenderKeyPair);
        ParliamentContractStub = GetTester<ParliamentContractContainer.ParliamentContractStub>(parliamentAddress,
            DefaultSenderKeyPair);

        var acs8Code = Codes.Single(kv => kv.Key.Contains("Tests.TestContract")).Value;
        Acs8ContractAddress = await DeployContractAsync(
            KernelConstants.CodeCoverageRunnerCategory,
            acs8Code,
            null,
            DefaultSenderKeyPair
        );
        Acs8ContractStub =
            GetTester<ContractContainer.ContractStub>(Acs8ContractAddress, DefaultSenderKeyPair);

        var feesCode = Codes.Single(kv => kv.Key.Contains("TransactionFees")).Value;
        TransactionFeesContractAddress = await DeployContractAsync(
            KernelConstants.CodeCoverageRunnerCategory,
            feesCode,
            null,
            DefaultSenderKeyPair
        );
        TransactionFeesContractStub =
            GetTester<TransactionFeesContractContainer.TransactionFeesContractStub>(TransactionFeesContractAddress,
                DefaultSenderKeyPair);

        //Consensus
        {
            var consensusCode = Codes.Single(kv => kv.Key.Contains("AEDPoS")).Value;
            var consensusContractAddress = await DeploySystemSmartContract(KernelConstants.CodeCoverageRunnerCategory,
                consensusCode,
                HashHelper.ComputeFrom("AElf.ContractNames.Consensus"), DefaultSenderKeyPair);
            AEDPoSContractStub =
                GetTester<AEDPoSContractContainer.AEDPoSContractStub>(consensusContractAddress,
                    DefaultSenderKeyPair);
        }
    }

    protected async Task InitializeTestContracts()
    {
        //initialize token
        {
            var createResult = await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = "ELF",
                Decimals = 8,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = Supply * 10,
                Issuer = DefaultSender,
                LockWhiteList =
                {
                    TokenConverterContractAddress,
                    TreasuryContractAddress
                },
                Owner = DefaultSender
            });
            CheckResult(createResult.TransactionResult);

            var issueResult = await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = "ELF",
                Amount = Supply / 2,
                To = DefaultSender,
                Memo = "Set for token converter."
            });
            CheckResult(issueResult.TransactionResult);

            foreach (var resourceRelatedNativeToken in NativTokenToSourceSymbols)
            {
                createResult = await TokenContractStub.Create.SendAsync(new CreateInput
                {
                    Symbol = resourceRelatedNativeToken,
                    Decimals = 8,
                    IsBurnable = true,
                    TokenName = resourceRelatedNativeToken + " elf token",
                    TotalSupply = Supply * 10,
                    Issuer = DefaultSender,
                    LockWhiteList =
                    {
                        TokenConverterContractAddress,
                        TreasuryContractAddress
                    },
                    Owner = DefaultSender
                });
                CheckResult(createResult.TransactionResult);
                issueResult = await TokenContractStub.Issue.SendAsync(new IssueInput
                {
                    Symbol = resourceRelatedNativeToken,
                    Amount = Supply / 2,
                    To = DefaultSender,
                    Memo = $"Set for {resourceRelatedNativeToken} token converter."
                });
                CheckResult(issueResult.TransactionResult);
            }

            foreach (var symbol in ResourceTokenSymbols)
            {
                var resourceCreateResult = await TokenContractStub.Create.SendAsync(new CreateInput
                {
                    Symbol = symbol,
                    TokenName = $"{symbol} Token",
                    TotalSupply = ResourceTokenTotalSupply,
                    Decimals = 8,
                    Issuer = DefaultSender,
                    IsBurnable = true,
                    LockWhiteList =
                    {
                        TokenConverterContractAddress,
                        TreasuryContractAddress
                    }
                });
                CheckResult(resourceCreateResult.TransactionResult);

                var resourceIssueResult = await TokenContractStub.Issue.SendAsync(new IssueInput
                {
                    Symbol = symbol,
                    To = TokenConverterContractAddress,
                    Amount = ResourceTokenTotalSupply,
                    Memo = "Initialize for resources trade"
                });
                CheckResult(resourceIssueResult.TransactionResult);
            }
        }

        //initialize parliament
        {
            var result = await ParliamentContractStub.Initialize.SendAsync(new InitializeInput());
            CheckResult(result.TransactionResult);
        }

        //initialize token converter
        {
            var connectors = new List<Connector>();
            var elfConnector = new Connector
            {
                Symbol = "ELF",
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = true,
                Weight = "0.5",
                VirtualBalance = VirtualElfSupplyToken
            };
            connectors.Add(elfConnector);
            foreach (var resourceRelatedNativeToken in NativTokenToSourceSymbols)
            {
                elfConnector = new Connector
                {
                    Symbol = resourceRelatedNativeToken,
                    IsPurchaseEnabled = true,
                    IsVirtualBalanceEnabled = true,
                    Weight = "0.5",
                    VirtualBalance = VirtualElfSupplyToken
                };
                elfConnector.RelatedSymbol = ResourceTokenSymbols.First(x => elfConnector.Symbol.Contains(x));
                connectors.Add(elfConnector);
            }

            foreach (var symbol in ResourceTokenSymbols)
            {
                var connector = new Connector
                {
                    Symbol = symbol,
                    VirtualBalance = ResourceTokenTotalSupply,
                    Weight = "0.5",
                    IsPurchaseEnabled = true,
                    IsVirtualBalanceEnabled = false
                };
                connector.RelatedSymbol = NativTokenToSourceSymbols.First(x => x.Contains(connector.Symbol));
                connectors.Add(connector);
            }

            await TokenConverterContractStub.Initialize.SendAsync(new Contracts.TokenConverter.InitializeInput
            {
                FeeRate = "0.005",
                Connectors = { connectors },
                BaseTokenSymbol = "ELF"
            });
        }

        //initialize treasury contract
        {
            await TreasuryContractStub.InitialTreasuryContract.SendAsync(new Empty());
            await TreasuryContractStub.InitialMiningRewardProfitItem.SendAsync(new Empty());
        }

        // initialize AEDPos
        {
            await InitializeAElfConsensus();
        }
    }

    protected void CheckResult(TransactionResult result)
    {
        if (result.Status != TransactionResultStatus.Mined)
            Assert.True(false, $"Status: {result.Status}, Message: {result.Error}");
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
                        { Pubkeys = { InitialCoreDataCenterKeyPairs.Select(p => ByteString.CopyFrom(p.PublicKey)) } },
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
            if (i == 0) minerInRound.IsExtraBlockProducer = true;

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
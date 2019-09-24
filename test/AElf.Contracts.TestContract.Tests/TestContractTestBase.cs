using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestContract.BasicFunction;
using AElf.Contracts.TestContract.BasicFunctionWithParallel;
using AElf.Contracts.TestContract.BasicSecurity;
using AElf.Contracts.TestContract.BasicUpdate;
using AElf.Contracts.TestContract.TransactionFees;
using AElf.Contracts.TestKit;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Mono.Cecil.Cil;
using Volo.Abp.Threading;
using Xunit;
using InitializeInput = AElf.Contracts.TokenConverter.InitializeInput;

namespace AElf.Contract.TestContract
{
    public class TestContractTestBase : ContractTestBase<TestContractAElfModule>
    {
        protected readonly Hash TestBasicFunctionContractSystemName =
            Hash.FromString("AElf.ContractNames.TestContract.BasicFunction");

        protected readonly Hash TestBasicSecurityContractSystemName =
            Hash.FromString("AElf.ContractNames.TestContract.BasicSecurity");

        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
        protected new Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();
        protected Address BasicFunctionContractAddress { get; set; }
        protected Address BasicSecurityContractAddress { get; set; }

        internal Acs0.ACS0Container.ACS0Stub BasicContractZeroStub { get; set; }

        internal BasicFunctionContractContainer.BasicFunctionContractStub TestBasicFunctionContractStub { get; set; }

        internal BasicSecurityContractContainer.BasicSecurityContractStub TestBasicSecurityContractStub { get; set; }

        internal Acs0.ACS0Container.ACS0Stub GetContractZeroTester(ECKeyPair keyPair)
        {
            return GetTester<Acs0.ACS0Container.ACS0Stub>(ContractZeroAddress, keyPair);
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

        protected void InitializeTestContracts()
        {
            BasicContractZeroStub = GetContractZeroTester(DefaultSenderKeyPair);

            //deploy test contract1
            BasicFunctionContractAddress = AsyncHelper.RunSync(async () =>
                await DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    Codes.Single(kv => kv.Key.EndsWith("BasicFunction")).Value,
                    TestBasicFunctionContractSystemName,
                    DefaultSenderKeyPair));
            TestBasicFunctionContractStub = GetTestBasicFunctionContractStub(DefaultSenderKeyPair);
            AsyncHelper.RunSync(async () => await InitialBasicFunctionContract());

            //deploy test contract2
            BasicSecurityContractAddress = AsyncHelper.RunSync(async () =>
                await DeploySystemSmartContract(
                    KernelConstants.CodeCoverageRunnerCategory,
                    Codes.Single(kv => kv.Key.EndsWith("BasicSecurity")).Value,
                    TestBasicSecurityContractSystemName,
                    DefaultSenderKeyPair));
            TestBasicSecurityContractStub = GetTestBasicSecurityContractStub(DefaultSenderKeyPair);
            AsyncHelper.RunSync(async () => await InitializeSecurityContract());
        }

        private async Task InitialBasicFunctionContract()
        {
            await TestBasicFunctionContractStub.InitialBasicFunctionContract.SendAsync(
                new AElf.Contracts.TestContract.BasicFunction.InitialBasicContractInput()
                {
                    ContractName = "Test Contract1",
                    MinValue = 10L,
                    MaxValue = 1000L,
                    MortgageValue = 1000_000_000L,
                    Manager = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[1].PublicKey)
                });
        }

        private async Task InitializeSecurityContract()
        {
            await TestBasicSecurityContractStub.InitialBasicSecurityContract.SendAsync(BasicFunctionContractAddress);
        }
    }

    public class TestFeesContractTestBase : ContractTestBase<TestFeesContractAElfModule>
    {
        protected const long Supply = 1000_0000_00000000L;
        protected ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs[0];
        protected Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
        protected ECKeyPair OtherTesterKeyPair => SampleECKeyPairs.KeyPairs[1];
        protected Address OtherTester => Address.FromPublicKey(OtherTesterKeyPair.PublicKey);
        protected ECKeyPair ManagerKeyPair { get; } = SampleECKeyPairs.KeyPairs[10];
        protected Address ManagerTester => Address.FromPublicKey(ManagerKeyPair.PublicKey);
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
        
        internal Kernel.SmartContract.ExecutionPluginForAcs8.Tests.TestContract.ContractContainer.ContractStub
            Acs8ContractStub { get; set; }

        internal TransactionFeesContractContainer.TransactionFeesContractStub TransactionFeesContractStub { get; set; }
        internal static readonly List<string> ResourceTokenSymbols = new List<string> {"RAM", "CPU", "NET", "STO"};

        protected async Task DeployTestContracts()
        {
            BasicContractZeroStub = GetTester<ACS0Container.ACS0Stub>(ContractZeroAddress, DefaultSenderKeyPair);

            var code = Codes.Single(kv => kv.Key.Contains("Profit")).Value;
            await DeploySystemSmartContract(KernelConstants.CodeCoverageRunnerCategory, code,
                ProfitSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
            
            TokenContractAddress = await DeploySystemSmartContract(
                KernelConstants.CodeCoverageRunnerCategory,
                Codes.Single(kv => kv.Key.EndsWith("MultiToken")).Value,
                SmartContractConstants.TokenContractSystemName,
                DefaultSenderKeyPair
            );
            TokenContractStub =
                GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultSenderKeyPair);

            TokenConverterContractAddress = await DeploySystemSmartContract(
                KernelConstants.CodeCoverageRunnerCategory,
                Codes.Single(kv => kv.Key.EndsWith("TokenConverter")).Value,
                SmartContractConstants.TokenConverterContractSystemName,
                DefaultSenderKeyPair
            );

            TreasuryContractAddress = await DeploySystemSmartContract(
                KernelConstants.CodeCoverageRunnerCategory,
                Codes.Single(kv => kv.Key.EndsWith("Treasury")).Value,
                SmartContractConstants.TreasuryContractSystemName,
                DefaultSenderKeyPair);
            TreasuryContractStub =
                GetTester<TreasuryContractContainer.TreasuryContractStub>(TreasuryContractAddress,
                    DefaultSenderKeyPair);

            TokenConverterContractStub =
                GetTester<TokenConverterContractContainer.TokenConverterContractStub>(TokenConverterContractAddress,
                    DefaultSenderKeyPair);

            var acs8Code = Codes.Single(kv => kv.Key.Contains("Tests.TestContract")).Value;
            Acs8ContractAddress = await DeployContractAsync(
                KernelConstants.CodeCoverageRunnerCategory,
                acs8Code,
                null,
                DefaultSenderKeyPair
            );
            Acs8ContractStub =
                GetTester<Kernel.SmartContract.ExecutionPluginForAcs8.Tests.TestContract.ContractContainer.
                    ContractStub>(Acs8ContractAddress, DefaultSenderKeyPair);

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
        }

        protected async Task InitializeTestContracts()
        {
            //initialize token
            {
                var createResult = await TokenContractStub.Create.SendAsync(new CreateInput()
                {
                    Symbol = "ELF",
                    Decimals = 2,
                    IsBurnable = true,
                    TokenName = "elf token",
                    TotalSupply = Supply,
                    Issuer = DefaultSender,
                    LockWhiteList =
                    {
                        TokenConverterContractAddress,
                        TreasuryContractAddress
                    }
                });
                CheckResult(createResult.TransactionResult);

                var issueResult = await TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = "ELF",
                    Amount = Supply / 2,
                    To = DefaultSender,
                    Memo = "Set for token converter."
                });
                CheckResult(issueResult.TransactionResult);

                issueResult = await TokenContractStub.Issue.SendAsync(new IssueInput()
                {
                    Symbol = "ELF",
                    Amount = Supply / 2,
                    To = OtherTester,
                    Memo = "Set for token converter."
                });
                CheckResult(issueResult.TransactionResult);

                foreach (var symbol in ResourceTokenSymbols)
                {
                    var resourceCreateResult = await TokenContractStub.Create.SendAsync(new CreateInput
                    {
                        Symbol = symbol,
                        TokenName = $"{symbol} Token",
                        TotalSupply = Supply,
                        Decimals = 2,
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
                        Amount = Supply,
                        Memo = "Initialize for resources trade"
                    });
                    CheckResult(resourceIssueResult.TransactionResult);
                }

                var setPriceResult = await TokenContractStub.SetResourceTokenUnitPrice.SendAsync(
                    new SetResourceTokenUnitPriceInput
                    {
                        CpuUnitPrice = 100L,
                        NetUnitPrice = 100L,
                        StoUnitPrice = 100L
                    });
                CheckResult(setPriceResult.TransactionResult);
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
                    VirtualBalance = Supply
                };
                connectors.Add(elfConnector);

                foreach (var symbol in ResourceTokenSymbols)
                {
                    var connector = new Connector
                    {
                        Symbol = symbol,
                        VirtualBalance = Supply,
                        Weight = "0.5",
                        IsPurchaseEnabled = true,
                        IsVirtualBalanceEnabled = false
                    };
                    connectors.Add(connector);
                }

                await TokenConverterContractStub.Initialize.SendAsync(new InitializeInput
                {
                    FeeRate = "0.005",
                    Connectors = {connectors},
                    BaseTokenSymbol = "ELF",
                    ManagerAddress = ManagerTester
                });
            }

            //initialize treasury contract
            {
                await TreasuryContractStub.InitialTreasuryContract.SendAsync(new Empty());
                await TreasuryContractStub.InitialMiningRewardProfitItem.SendAsync(new Empty());
            }
        }

        protected void CheckResult(TransactionResult result)
        {
            if (result.Status != TransactionResultStatus.Mined)
            {
                Assert.True(false, $"Status: {result.Status}, Message: {result.Error}");
            }
        }
    }
}
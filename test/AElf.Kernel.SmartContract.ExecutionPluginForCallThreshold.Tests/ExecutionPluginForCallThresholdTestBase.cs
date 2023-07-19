using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Parliament;
using AElf.Contracts.TokenConverter;
using AElf.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.EconomicSystem;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract.ExecutionPluginForCallThreshold.Tests.TestContract;
using AElf.Kernel.Token;
using AElf.Types;
using Shouldly;
using InitializeInput = AElf.Contracts.TokenConverter.InitializeInput;

namespace AElf.Kernel.SmartContract.ExecutionPluginForCallThreshold.Tests;

public class ExecutionPluginForCallThresholdTestBase : ContractTestBase<ExecutionPluginForCallThresholdTestModule>
{
    //init connector
    internal Connector ELFConnector = new()
    {
        Symbol = "ELF",
        VirtualBalance = 100_0000,
        Weight = "0.5",
        IsPurchaseEnabled = true,
        IsVirtualBalanceEnabled = true
    };

    internal Connector WriteConnector = new()
    {
        Symbol = "WRITE",
        VirtualBalance = 0,
        Weight = "0.5",
        IsPurchaseEnabled = true,
        IsVirtualBalanceEnabled = false
    };

    internal Address TestContractAddress { get; set; }
    internal Address TokenContractAddress { get; set; }
    internal Address TokenConverterAddress { get; set; }
    internal ContractContainer.ContractStub DefaultTester { get; set; }
    internal TokenContractContainer.TokenContractStub TokenContractStub { get; set; }
    internal TokenConverterContractContainer.TokenConverterContractStub TokenConverterContractStub { get; set; }
    internal Address ParliamentAddress;
    internal ParliamentContractImplContainer.ParliamentContractImplStub ParliamentContractStub;

    internal ECKeyPair DefaultSenderKeyPair => Accounts[0].KeyPair;
    internal ECKeyPair OtherTester => Accounts[1].KeyPair;
    internal Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
    protected ECKeyPair FeeReceiverKeyPair => Accounts[10].KeyPair;
    protected Address FeeReceiverAddress => Accounts[10].Address;
    protected ECKeyPair ManagerKeyPair => Accounts[11].KeyPair;
    protected Address ManagerAddress => Accounts[11].Address;

    protected async Task InitializeContracts()
    {
        await DeployContractsAsync();
        await InitializedParliament();
        await InitializeTokenAsync();
        await InitializeTokenConverterAsync();
    }

    private async Task DeployContractsAsync()
    {
        const int category = KernelConstants.CodeCoverageRunnerCategory;
        //Token contract
        {
            var code = Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
            TokenContractAddress = await DeploySystemSmartContract(category, code,
                TokenSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
            TokenContractStub =
                GetTester<TokenContractContainer.TokenContractStub>(TokenContractAddress, DefaultSenderKeyPair);
        }

        //Token converter
        {
            var code = Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value;
            TokenConverterAddress = await DeploySystemSmartContract(category, code,
                TokenConverterSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
            TokenConverterContractStub =
                GetTester<TokenConverterContractContainer.TokenConverterContractStub>(TokenConverterAddress,
                    DefaultSenderKeyPair);
        }

        //Test contract
        {
            var code = Codes.Single(kv => kv.Key.Contains("ExecutionPluginForCallThreshold.Tests.TestContract")).Value;
            TestContractAddress = await DeployContractAsync(category, code, HashHelper.ComputeFrom("TestContract"),
                DefaultSenderKeyPair);
            DefaultTester =
                GetTester<ContractContainer.ContractStub>(TestContractAddress, DefaultSenderKeyPair);
        }
        // Parliament
        {
            var code = Codes.Single(kv => kv.Key.Contains("MockParliament")).Value;
            ParliamentAddress = await DeploySystemSmartContract(category, code,
                ParliamentSmartContractAddressNameProvider.Name, DefaultSenderKeyPair);
            ParliamentContractStub =
                GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentAddress,
                    DefaultSenderKeyPair);
        }
    }
    
    private async Task InitializedParliament()
    {
        await ParliamentContractStub.Initialize.SendAsync(new AElf.Contracts.Parliament.InitializeInput
        {
            ProposerAuthorityRequired = false,
            PrivilegedProposer = DefaultSender
        });
    }

    private async Task InitializeTokenAsync()
    {
        //init elf token
        {
            var createResult = await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = "ELF",
                Decimals = 2,
                IsBurnable = true,
                TokenName = "elf token",
                TotalSupply = 1000_00000000L,
                Issuer = DefaultSender,
                Owner = DefaultSender
            });

            createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var issueResult = await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = "ELF",
                Amount = 1000_00000000L,
                To = DefaultSender,
                Memo = "Set for elf token converter."
            });
            issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        //init resource token
        {
            var createResult = await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = "WRITE",
                Decimals = 2,
                IsBurnable = true,
                TokenName = "WRITE token",
                TotalSupply = 1000_0000L,
                Issuer = DefaultSender,
                Owner = DefaultSender
            });

            createResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var issueResult = await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Symbol = "WRITE",
                Amount = 1000_000L,
                To = DefaultSender,
                Memo = "Set for net token converter."
            });
            issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }

    private async Task InitializeTokenConverterAsync()
    {
        var input = new InitializeInput
        {
            BaseTokenSymbol = "WRITE",
            FeeRate = "0.005",
            Connectors = { ELFConnector, WriteConnector }
        };

        var initializeResult = await TokenConverterContractStub.Initialize.SendAsync(input);
        initializeResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }
}
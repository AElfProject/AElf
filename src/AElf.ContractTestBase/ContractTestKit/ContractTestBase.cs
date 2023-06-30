using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Parliament;
using AElf.CrossChain;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.CSharp.Core.Extension;
using AElf.EconomicSystem;
using AElf.GovernmentSystem;
using AElf.Kernel;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Standards.ACS0;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Testing;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.ContractTestBase.ContractTestKit;

public class ContractTestBase<TModule> : AbpIntegratedTest<TModule> where TModule : AbpModule
{
    private readonly IContractTestKitFactory _contractTestKitFactory;
    private readonly IContractTestService _contractTestService;


    public ContractTestBase()
    {
        _contractTestKitFactory = Application.ServiceProvider.GetRequiredService<IContractTestKitFactory>();
        _contractTestService = Application.ServiceProvider.GetRequiredService<IContractTestService>();

        AsyncHelper.RunSync(InitSystemContractAddressesAsync);
    }

    protected Account DefaultAccount => Accounts[0];
    protected IReadOnlyList<Account> Accounts => SampleAccount.Accounts;

    protected int InitialCoreDataCenterCount => 5;
    
    protected List<ECKeyPair> InitialCoreDataCenterKeyPairs =>
        Accounts.Take(InitialCoreDataCenterCount).Select(a => a.KeyPair).ToList();

    protected Dictionary<Hash, Address> SystemContractAddresses { get; } = new();

    protected Address BasicContractZeroAddress =>
        SystemContractAddresses[ZeroSmartContractAddressNameProvider.Name];

    protected Address CrossChainContractAddress =>
        SystemContractAddresses[CrossChainSmartContractAddressNameProvider.Name];

    protected Address TokenContractAddress => SystemContractAddresses[TokenSmartContractAddressNameProvider.Name];

    protected Address ParliamentContractAddress =>
        SystemContractAddresses[ParliamentSmartContractAddressNameProvider.Name];

    protected Address ConsensusContractAddress =>
        SystemContractAddresses[ConsensusSmartContractAddressNameProvider.Name];

    protected Address ReferendumContractAddress =>
        SystemContractAddresses[ReferendumSmartContractAddressNameProvider.Name];

    protected Address TreasuryContractAddress =>
        SystemContractAddresses[TreasurySmartContractAddressNameProvider.Name];

    protected Address AssociationContractAddress =>
        SystemContractAddresses[AssociationSmartContractAddressNameProvider.Name];

    protected Address TokenConverterContractAddress =>
        SystemContractAddresses[TokenConverterSmartContractAddressNameProvider.Name];

    public ISmartContractAddressService ContractAddressService =>
        Application.ServiceProvider.GetRequiredService<ISmartContractAddressService>();

    protected override void SetAbpApplicationCreationOptions(AbpApplicationCreationOptions options)
    {
        options.UseAutofac();
    }

    private async Task InitSystemContractAddressesAsync()
    {
        var blockchainService = Application.ServiceProvider.GetService<IBlockchainService>();
        var chain = await blockchainService.GetChainAsync();
        var block = await blockchainService.GetBlockByHashAsync(chain.GenesisBlockHash);
        var transactionResultManager = Application.ServiceProvider.GetService<ITransactionResultManager>();
        var transactionResults =
            await transactionResultManager.GetTransactionResultsAsync(block.Body.TransactionIds, block.GetHash());
        foreach (var transactionResult in transactionResults)
        {
            Assert.True(transactionResult.Status == TransactionResultStatus.Mined, transactionResult.Error);
            var relatedLogs = transactionResult.Logs.Where(l => l.Name == nameof(ContractDeployed)).ToList();
            if (!relatedLogs.Any()) break;
            foreach (var relatedLog in relatedLogs)
            {
                var eventData = new ContractDeployed();
                eventData.MergeFrom(relatedLog);
                SystemContractAddresses[eventData.Name] = eventData.Address;
            }
        }
    }

    protected ContractTestKit<T> CreateContractTestKit<T>(ChainInitializationDto dto) where T : AbpModule
    {
        var application = CreateApplication<T>(dto);
        return _contractTestKitFactory.Create<T>(application);
    }

    protected T GetTester<T>(Address contractAddress, ECKeyPair senderKey = null) where T : ContractStubBase, new()
    {
        return _contractTestService.GetTester<T>(contractAddress, senderKey ?? DefaultAccount.KeyPair);
    }

    /// <summary>
    ///     Mine a block with given normal txs and system txs.
    ///     Normal txs will use tx pool while system txs not.
    /// </summary>
    /// <param name="txs"></param>
    /// <param name="blockTime"></param>
    /// <returns></returns>
    protected async Task<BlockExecutedSet> MineAsync(List<Transaction> txs, Timestamp blockTime = null)
    {
        return await _contractTestService.MineAsync(txs, blockTime);
    }

    protected async Task<TransactionResult> ExecuteTransactionWithMiningAsync(Transaction transaction,
        Timestamp blockTime = null)
    {
        return await _contractTestService.ExecuteTransactionWithMiningAsync(transaction, blockTime);
    }

    private IAbpApplication CreateApplication<T>(ChainInitializationDto dto) where T : AbpModule
    {
        var application =
            AbpApplicationFactory.Create<T>(options =>
            {
                options.UseAutofac();
                options.Services.Configure<ChainOptions>(o => { o.ChainId = dto.ChainId; });

                options.Services.Configure<ChainInitializationOptions>(o =>
                {
                    o.Symbol = dto.Symbol;
                    o.ChainId = dto.ChainId;
                    o.ParentChainId = dto.ParentChainId;
                    o.CreationHeightOnParentChain = dto.CreationHeightOnParentChain;
                    o.ParentChainTokenContractAddress = dto.ParentChainTokenContractAddress;
                    o.RegisterParentChainTokenContractAddress = dto.RegisterParentChainTokenContractAddress;
                });
            });

        application.Initialize();
        return application;
    }
    
    protected async Task SubmitAndApproveProposalOfDefaultParliament(Address contractAddress, string methodName,
        IMessage message)
    {
        var parliamentContractStub = GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress, DefaultAccount.KeyPair);
        var defaultParliamentAddress =
            await parliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        
        var proposal = new CreateProposalInput
        {
            OrganizationAddress = defaultParliamentAddress,
            ContractMethodName = methodName,
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
            Params = message.ToByteString(),
            ToAddress = contractAddress
        };
        var createResult = await parliamentContractStub.CreateProposal.SendAsync(proposal);
        var proposalId = createResult.Output;
        await ApproveWithMinersAsync(proposalId);
        await parliamentContractStub.Release.SendAsync(proposalId);
    }

    private async Task ApproveWithMinersAsync(Hash proposalId)
    {
        foreach (var bp in InitialCoreDataCenterKeyPairs)
        {
            var tester = GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress, bp);
            await tester.Approve.SendAsync(proposalId);
        }
    }
}
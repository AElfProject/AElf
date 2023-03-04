using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Parliament;
using AElf.Kernel.CodeCheck.Infrastructure;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract.Application;

namespace AElf.Kernel.CodeCheck.Application;

internal class CodeCheckProposalService:ICodeCheckProposalService,ITransientDependency
{
    private readonly IContractReaderFactory<ParliamentContractContainer.ParliamentContractStub>
        _contractReaderFactory;
    private readonly ICodeCheckProposalProvider _codeCheckProposalProvider;
    private readonly ISmartContractAddressService _smartContractAddressService;
    private readonly ICodeCheckReleasedProposalIdProvider _codeCheckReleasedProposalIdProvider;
    
    public ILogger<CodeCheckProposalService> Logger { get; set; }

    public CodeCheckProposalService(ICodeCheckProposalProvider codeCheckProposalProvider,
        ISmartContractAddressService smartContractAddressService,
        IContractReaderFactory<ParliamentContractContainer.ParliamentContractStub> contractReaderFactory,
        ICodeCheckReleasedProposalIdProvider codeCheckReleasedProposalIdProvider)
    {
        _codeCheckProposalProvider = codeCheckProposalProvider;
        _smartContractAddressService = smartContractAddressService;
        _contractReaderFactory = contractReaderFactory;
        _codeCheckReleasedProposalIdProvider = codeCheckReleasedProposalIdProvider;
    }

    public void AddToReleasedProposal(Hash proposalId, Hash proposalInputHash, long height)
    {
        _codeCheckProposalProvider.AddProposal(proposalId, proposalInputHash, height);
    }

    public async Task<List<CodeCheckProposal>> GetToReleasedProposalListAsync(Address @from, Hash blockHash, long blockHeight)
    {
        var proposalList = _codeCheckProposalProvider.GetAllProposals();
        var result = await _contractReaderFactory.Create(new ContractReaderContext
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            ContractAddress = await GetParliamentContractAddressAsync(new ChainContext
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight
            }),
            Sender = from
        }).GetReleaseThresholdReachedProposals.CallAsync(new ProposalIdList
            { ProposalIds = { proposalList.Select(o => o.ProposalId).ToList() } });

        if (result == null || result.ProposalIds.Count == 0)
        {
            return null;
        }

        return proposalList.Where(o => result.ProposalIds.Contains(o.ProposalId)).ToList();
    }

    public async Task ClearProposalByLibAsync(Hash blockHash, long blockHeight)
    {
        var proposalList = _codeCheckProposalProvider.GetAllProposals();
        var proposalIdList = proposalList.Select(o => o.ProposalId).ToList();
        var result = await _contractReaderFactory.Create(new ContractReaderContext
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight,
                ContractAddress = await GetParliamentContractAddressAsync(new ChainContext
                {
                    BlockHash = blockHash,
                    BlockHeight = blockHeight
                })
            }).GetAvailableProposals
            .CallAsync(new ProposalIdList { ProposalIds = { proposalIdList } });

        if (result == null)
            return;

        foreach (var proposalId in proposalIdList.Except(result.ProposalIds))
        {
            if (!_codeCheckProposalProvider.TryGetProposalCreatedHeight(proposalId, out var h) ||
                h > blockHeight)
                continue;
            Logger.LogDebug($"Clear code check proposal {proposalId} by LIB hash {blockHash}, height {blockHeight}");
            _codeCheckProposalProvider.RemoveProposalById(proposalId);
            
            await _codeCheckReleasedProposalIdProvider.RemoveProposalIdAsync(new BlockIndex
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight
            }, proposalId);
        }
    }
    
    private Task<Address> GetParliamentContractAddressAsync(IChainContext chainContext)
    {
        return _smartContractAddressService.GetAddressByContractNameAsync(chainContext,
            ParliamentSmartContractAddressNameProvider.StringName);
    }
}
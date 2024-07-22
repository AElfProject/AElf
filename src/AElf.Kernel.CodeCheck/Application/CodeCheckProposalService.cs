using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Parliament;
using AElf.Kernel.CodeCheck.Infrastructure;
using AElf.Kernel.Proposal;
using AElf.Kernel.SmartContract.Application;

namespace AElf.Kernel.CodeCheck.Application;

internal class CodeCheckProposalService : ICodeCheckProposalService, ITransientDependency
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

    public void AddReleasableProposal(Hash proposalId, Hash proposalInputHash, long height)
    {
        _codeCheckProposalProvider.AddProposal(proposalId, proposalInputHash, height);
    }

    public async Task<List<CodeCheckProposal>> GetReleasableProposalListAsync(Address from, Hash blockHash,
        long blockHeight)
    {
        var allOpenProposals = _codeCheckProposalProvider.GetAllProposals().ToList();
        if (allOpenProposals.Count == 0) return null;
        
        var releaseThresholdReachedProposals = await _contractReaderFactory.Create(new ContractReaderContext
        {
            BlockHash = blockHash,
            BlockHeight = blockHeight,
            ContractAddress = await GetParliamentContractAddressAsync(new ChainContext
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight
            })
        }).GetReleaseThresholdReachedProposals.CallAsync(new ProposalIdList
            { ProposalIds = { allOpenProposals.Select(o => o.ProposalId).ToList() } });

        if (releaseThresholdReachedProposals == null || releaseThresholdReachedProposals.ProposalIds.Count == 0)
        {
            return null;
        }

        return allOpenProposals.Where(o => releaseThresholdReachedProposals.ProposalIds.Contains(o.ProposalId)).ToList();
    }

    public async Task ClearProposalByLibAsync(Hash blockHash, long blockHeight)
    {
        var stillOpenProposals = _codeCheckProposalProvider.GetAllProposals().Select(o => o.ProposalId).ToList();
        if (stillOpenProposals.Count == 0) return;
        
        var releaseThresholdReachedProposals = await _contractReaderFactory.Create(new ContractReaderContext
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight,
                ContractAddress = await GetParliamentContractAddressAsync(new ChainContext
                {
                    BlockHash = blockHash,
                    BlockHeight = blockHeight
                })
            }).GetAvailableProposals
            .CallAsync(new ProposalIdList { ProposalIds = { stillOpenProposals } });

        foreach (var proposalId in stillOpenProposals.Except(releaseThresholdReachedProposals.ProposalIds))
        {
            if (!_codeCheckProposalProvider.TryGetProposalCreatedHeight(proposalId, out var h) ||
                h > blockHeight)
                continue;
            Logger.LogDebug("Clear code check proposal {proposalId} by LIB hash {blockHash}, height {blockHeight}",
                proposalId.ToHex(), blockHash.ToHex(), blockHeight);

            await _codeCheckReleasedProposalIdProvider.RemoveProposalIdAsync(new BlockIndex
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight
            }, proposalId);
            _codeCheckProposalProvider.RemoveProposalById(proposalId);
        }
    }

    private Task<Address> GetParliamentContractAddressAsync(IChainContext chainContext)
    {
        return _smartContractAddressService.GetAddressByContractNameAsync(chainContext,
            ParliamentSmartContractAddressNameProvider.StringName);
    }
}
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Parliament;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Proposal;
using AElf.Kernel.Proposal.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Txn.Application;
using AElf.Standards.ACS0;
using Google.Protobuf;

namespace AElf.Kernel.CodeCheck.Application;

internal class CodeCheckProposalReleaseTransactionGenerator : ISystemTransactionGenerator
{
    private readonly ICodeCheckProposalService _codeCheckProposalService;
    private readonly ISmartContractAddressService _smartContractAddressService;
    private readonly ITransactionPackingOptionProvider _transactionPackingOptionProvider;
    private readonly ICodeCheckReleasedProposalIdProvider _codeCheckReleasedProposalIdProvider;

    public CodeCheckProposalReleaseTransactionGenerator(ICodeCheckProposalService codeCheckProposalService,
        ISmartContractAddressService smartContractAddressService,
        ITransactionPackingOptionProvider transactionPackingOptionProvider,
        ICodeCheckReleasedProposalIdProvider codeCheckReleasedProposalIdProvider)
    {
        _codeCheckProposalService = codeCheckProposalService;
        _smartContractAddressService = smartContractAddressService;
        _transactionPackingOptionProvider = transactionPackingOptionProvider;
        _codeCheckReleasedProposalIdProvider = codeCheckReleasedProposalIdProvider;

        Logger = NullLogger<ProposalApprovalTransactionGenerator>.Instance;
    }

    public ILogger<ProposalApprovalTransactionGenerator> Logger { get; set; }

    public async Task<List<Transaction>> GenerateTransactionsAsync(Address from, long preBlockHeight, Hash preBlockHash)
    {
        var chainContext = new ChainContext
        {
            BlockHash = preBlockHash, BlockHeight = preBlockHeight
        };
        if (!_transactionPackingOptionProvider.IsTransactionPackable(chainContext)) return new List<Transaction>();

        var zeroContractAddress = _smartContractAddressService.GetZeroSmartContractAddress();

        if (zeroContractAddress == null) return new List<Transaction>();

        var releasableProposals =
            await _codeCheckProposalService.GetReleasableProposalListAsync(from, preBlockHash, preBlockHeight);
        if (releasableProposals == null || releasableProposals.Count == 0) return new List<Transaction>();

        var alreadyReleased = (await _codeCheckReleasedProposalIdProvider.GetProposalIdsAsync(new BlockIndex
        {
            BlockHash = preBlockHash,
            BlockHeight = preBlockHeight
        })).ProposalIds.ToHashSet();

        var releaseRequired = releasableProposals.Where(o => !alreadyReleased.Contains(o.ProposalId)).ToList();

        var releaseContractTransactions = releaseRequired.Select(proposal => new Transaction
        {
            From = from,
            MethodName = nameof(ACS0Container.ACS0Stub.ReleaseApprovedUserSmartContract),
            To = zeroContractAddress,
            RefBlockNumber = preBlockHeight,
            RefBlockPrefix = BlockHelper.GetRefBlockPrefix(preBlockHash),
            Params = new ReleaseContractInput
            {
                ProposalId = proposal.ProposalId,
                ProposedContractInputHash = proposal.ProposedContractInputHash
            }.ToByteString()
        }).ToList();

        await _codeCheckReleasedProposalIdProvider.AddProposalIdsAsync(new BlockIndex
        {
            BlockHash = preBlockHash,
            BlockHeight = preBlockHeight
        }, releaseRequired.Select(p => p.ProposalId).ToList());

        return releaseContractTransactions;
    }
}
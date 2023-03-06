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

    public async Task<List<Transaction>> GenerateTransactionsAsync(Address from, long preBlockHeight,
        Hash preBlockHash)
    {
        var generatedTransactions = new List<Transaction>();
        var chainContext = new ChainContext
        {
            BlockHash = preBlockHash, BlockHeight = preBlockHeight
        };
        if (!_transactionPackingOptionProvider.IsTransactionPackable(chainContext))
            return generatedTransactions;

        var zeroContractAddress = _smartContractAddressService.GetZeroSmartContractAddress();

        if (zeroContractAddress == null) return generatedTransactions;

        var proposalList =
            await _codeCheckProposalService.GetToReleasedProposalListAsync(from, preBlockHash, preBlockHeight);
        if (proposalList == null || proposalList.Count == 0)
            return generatedTransactions;

        var releasedProposalList = await _codeCheckReleasedProposalIdProvider.GetProposalIdsAsync(new BlockIndex
        {
            BlockHash = preBlockHash,
            BlockHeight = preBlockHeight
        });
        if (releasedProposalList.ProposalIds.Count != 0)
        {
            proposalList = proposalList.Where(o => !releasedProposalList.ProposalIds.Contains(o.ProposalId)).ToList();
        }

        foreach (var proposal in proposalList)
        {
            var generatedTransaction = new Transaction
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
            };
            generatedTransactions.Add(generatedTransaction);

            await _codeCheckReleasedProposalIdProvider.AddProposalIdAsync(new BlockIndex
            {
                BlockHash = preBlockHash,
                BlockHeight = preBlockHeight
            }, proposal.ProposalId);
            
            Logger.LogTrace("Code check proposal release transaction generated: {proposalId}.",proposal.ProposalId.ToHex());
        }
        
        return generatedTransactions;
    }
}
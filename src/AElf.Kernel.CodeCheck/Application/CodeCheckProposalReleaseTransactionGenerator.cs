using System.Collections.Generic;
using AElf.Contracts.Parliament;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.Proposal;
using AElf.Kernel.Proposal.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Txn.Application;
using AElf.Standards.ACS0;
using Google.Protobuf;

namespace AElf.Kernel.CodeCheck.Application;

public class CodeCheckProposalReleaseTransactionGenerator : ISystemTransactionGenerator
{
    private readonly ICodeCheckProposalService _codeCheckProposalService;
    private readonly ISmartContractAddressService _smartContractAddressService;
    private readonly ITransactionPackingOptionProvider _transactionPackingOptionProvider;

    public CodeCheckProposalReleaseTransactionGenerator(ICodeCheckProposalService codeCheckProposalService,
        ISmartContractAddressService smartContractAddressService,
        ITransactionPackingOptionProvider transactionPackingOptionProvider)
    {
        _codeCheckProposalService = codeCheckProposalService;
        _smartContractAddressService = smartContractAddressService;
        _transactionPackingOptionProvider = transactionPackingOptionProvider;

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
        }
        
        Logger.LogTrace("Code check proposal release transaction generated.");

        return generatedTransactions;
    }
}
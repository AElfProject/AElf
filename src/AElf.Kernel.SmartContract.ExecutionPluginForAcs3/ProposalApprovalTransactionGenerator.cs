using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.ParliamentAuth;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.TransactionPool.Application;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs3
{
    public class ProposalApprovalTransactionGenerator : ISystemTransactionGenerator
    {
        private readonly IProposalService _proposalService;
        private readonly ITransactionPackingService _transactionPackingService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        
        public ILogger<ProposalApprovalTransactionGenerator> Logger { get; set; }

        public ProposalApprovalTransactionGenerator(IProposalService proposalService, 
            ISmartContractAddressService smartContractAddressService, ITransactionPackingService transactionPackingService)
        {
            _proposalService = proposalService;
            _smartContractAddressService = smartContractAddressService;
            _transactionPackingService = transactionPackingService;
            
            Logger = NullLogger<ProposalApprovalTransactionGenerator>.Instance;
        }
        
        public async Task<List<Transaction>> GenerateTransactionsAsync(Address from, long preBlockHeight, Hash preBlockHash)
        {
            var generatedTransactions = new List<Transaction>();
            if (!_transactionPackingService.IsTransactionPackingEnabled())
                return generatedTransactions;
            
            var parliamentAuthContractAddress = _smartContractAddressService.GetAddressByContractName(
                ParliamentAuthSmartContractAddressNameProvider.Name);

            if (parliamentAuthContractAddress == null)
            {
                return generatedTransactions;
            }

            var proposalIdList = await _proposalService.GetNotApprovedProposalIdListAsync(preBlockHash, preBlockHeight);
            if (proposalIdList == null || proposalIdList.Count == 0) 
                return generatedTransactions;
            
            var generatedTransaction = new Transaction
            {
                From = @from,
                MethodName = nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.ApproveMultiProposals),
                To = parliamentAuthContractAddress,
                RefBlockNumber = preBlockHeight,
                RefBlockPrefix = ByteString.CopyFrom(preBlockHash.Value.Take(4).ToArray()),
                Params = new ProposalIdList
                {
                    ProposalIds = {proposalIdList}
                }.ToByteString()
            };
            generatedTransactions.Add(generatedTransaction);
            
            Logger.LogInformation("Proposal approval transaction generated.");

            return generatedTransactions;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using Acs3;
using AElf.Contracts.ParliamentAuth;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.Miner.Application
{
    public class ProposalApprovalTransactionGenerator : ISystemTransactionGenerator
    {
        private readonly IReadyToApproveProposalCacheProvider _readyToApproveProposalCacheProvider;
        
        private readonly ISmartContractAddressService _smartContractAddressService;

        public ProposalApprovalTransactionGenerator(IReadyToApproveProposalCacheProvider readyToApproveProposalCacheProvider, 
            ISmartContractAddressService smartContractAddressService)
        {
            _readyToApproveProposalCacheProvider = readyToApproveProposalCacheProvider;
            _smartContractAddressService = smartContractAddressService;
        }
        
        public void GenerateTransactions(Address from, long preBlockHeight, Hash preBlockHash,
            ref List<Transaction> generatedTransactions)
        {
            var parliamentAuthContractAddress = _smartContractAddressService.GetAddressByContractName(
                ParliamentAuthSmartContractAddressNameProvider.Name);

            if (parliamentAuthContractAddress == null)
            {
                return;
            }

            while (_readyToApproveProposalCacheProvider.TryGetProposalToApprove(out var proposalId))
            {
                generatedTransactions.Add(
                    new Transaction
                        {
                            From = from,
                            MethodName = nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve),
                            To = parliamentAuthContractAddress,
                            RefBlockNumber = preBlockHeight,
                            RefBlockPrefix = ByteString.CopyFrom(preBlockHash.Value.Take(4).ToArray()),
                            Params = new ApproveInput
                            {
                                ProposalId = proposalId
                            }.ToByteString()
                        });
            }
        }
    }
}

using AElf.Kernel;
using AElf.Kernel.Types;

namespace AElf.Crosschain
{
    public class CrossChainTransactionTypeIdentifier : ITransactionTypeIdentifier
    {
        private static bool IsIndexingSideChainTransaction(int chainId, Transaction transaction)
        {
            return transaction.To.Equals(ContractHelpers.GetCrossChainContractAddress(chainId)) &&
                   transaction.MethodName.Equals(TypeConsts.IndexingSideChainMethodName);
        }

        private bool IsIndexingParentChainTransaction(int chainId, Transaction transaction)
        {
            return transaction.To.Equals(ContractHelpers.GetCrossChainContractAddress(chainId)) &&
                   transaction.MethodName.Equals(TypeConsts.IndexingParentChainMethodName);
        }
        
        private bool IsCrossChainIndexingTransaction(int chainId, Transaction transaction)
        {
            return IsIndexingParentChainTransaction(chainId, transaction) || IsIndexingSideChainTransaction(chainId, transaction);
        }

        public bool IsSystemTransaction(int chainId, Transaction transaction)
        {
            return IsCrossChainIndexingTransaction(chainId, transaction);
        }

        public bool CanBeBroadCast(int chainId, Transaction transaction)
        {
            return !IsCrossChainIndexingTransaction(chainId, transaction);
        }
    }
}
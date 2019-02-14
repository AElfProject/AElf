using AElf.Kernel.Types;

namespace AElf.Kernel.Extensions
{
    public static class TransactionTypeExtensions
    {
        public static bool IsDposTransaction(this Transaction transaction, int chainId)
        {
            return transaction.To.Equals(ContractHelpers.GetConsensusContractAddress(chainId));
        }

        public static bool IsIndexingSideChainTransaction(this Transaction transaction, int chainId)
        {
            return transaction.To.Equals(ContractHelpers.GetCrossChainContractAddress(chainId)) &&
                   transaction.MethodName.Equals(ContractHelpers.IndexingSideChainMethodName);
        }

        public static bool IsIndexingParentChainTransaction(this Transaction transaction, int chainId)
        {
            return transaction.To.Equals(ContractHelpers.GetCrossChainContractAddress(chainId)) &&
                   transaction.MethodName.Equals(ContractHelpers.IndexingParentChainMethodName);
        }

        public static bool IsCrossChainIndexingTransaction(this Transaction transaction, int chainId)
        {
            return transaction.IsIndexingParentChainTransaction(chainId) || transaction.IsIndexingSideChainTransaction(chainId);
        }

        public static bool IsSystemTransaction(this Transaction transaction, int chainId)
        {
            return transaction.IsDposTransaction(chainId) || transaction.IsCrossChainIndexingTransaction(chainId);
        }

        public static bool IsClaimFeesTransaction(this Transaction transaction, int chainId)
        {
            return transaction.To.Equals(ContractHelpers.GetTokenContractAddress(chainId)) &&
                   transaction.MethodName.Equals("ClaimTransactionFees");
        }
    }
}
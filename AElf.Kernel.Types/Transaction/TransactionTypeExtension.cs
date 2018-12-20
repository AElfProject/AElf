using AElf.Common;
using AElf.Configuration.Config.Chain;

namespace AElf.Kernel.Types.Transaction
{
    public static class TransactionTypeExtension
    {
        private static Hash ChainId { get; } = Hash.LoadBase58(ChainConfig.Instance.ChainId);
        private static Address DPosContractAddress { get; } = ContractHelpers.GetConsensusContractAddress(ChainId);

        private static Address CrossChainContractAddress { get; } =
            ContractHelpers.GetCrossChainContractAddress(ChainId);

        public static bool IsDposTransaction(this Kernel.Transaction transaction)
        {
            return transaction.To.Equals(DPosContractAddress);
        }

        public static bool IsIndexingSideChainTransaction(this Kernel.Transaction transaction)
        {
            return transaction.To.Equals(CrossChainContractAddress) &&
                   transaction.MethodName.Equals(ContractHelpers.IndexingSideChainMethodName);
        }
        
        public static bool IsIndexingParentChainTransaction(this Kernel.Transaction transaction)
        {
            return transaction.To.Equals(CrossChainContractAddress) &&
                   transaction.MethodName.Equals(ContractHelpers.IndexingParentChainMethodName);
        }

        public static bool IsCrossChainIndexingTransaction(this Kernel.Transaction transaction)
        {
            return transaction.IsIndexingParentChainTransaction() || transaction.IsIndexingSideChainTransaction();
        }
        
        public static bool IsSystemTransaction(this Kernel.Transaction transaction)
        {
            return transaction.IsDposTransaction() || transaction.IsCrossChainIndexingTransaction();
        }
    }
}
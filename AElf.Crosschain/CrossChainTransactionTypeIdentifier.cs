using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Kernel;
using AElf.Kernel.Txn;
using AElf.Kernel.Types;

namespace AElf.Crosschain
{
    public class CrossChainTransactionTypeIdentifier : ITransactionTypeIdentifier
    {
        private static int ChainId { get; } = ChainConfig.Instance.ChainId.ConvertBase58ToChainId();
        private static Address CrossChainContractAddress { get; } =
            ContractHelpers.GetCrossChainContractAddress(ChainId);
        private static bool IsIndexingSideChainTransaction(Transaction transaction)
        {
            return transaction.To.Equals(CrossChainContractAddress) &&
                   transaction.MethodName.Equals(TypeConsts.IndexingSideChainMethodName);
        }

        private bool IsIndexingParentChainTransaction(Transaction transaction)
        {
            return transaction.To.Equals(CrossChainContractAddress) &&
                   transaction.MethodName.Equals(TypeConsts.IndexingParentChainMethodName);
        }
        
        private bool IsCrossChainIndexingTransaction(Transaction transaction)
        {
            return IsIndexingParentChainTransaction(transaction) || IsIndexingSideChainTransaction(transaction);
        }

        public bool IsSystemTransaction(Transaction transaction)
        {
            return IsCrossChainIndexingTransaction(transaction);
        }

        public bool CanBeBroadCast(Transaction transaction)
        {
            return !IsCrossChainIndexingTransaction(transaction);
        }
    }
}
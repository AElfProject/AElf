using AElf.Kernel.Types;

namespace AElf.Kernel
{
    /*
    public static class TransactionTypeExtensions
    {
        public static bool IsDposTransaction(this Transaction transaction, int chainId)
        {
            return transaction.To.Equals(ContractHelpers.GetConsensusContractAddress(chainId));
        }

        public static bool IsSystemTransaction(this Kernel.Transaction transaction, int chainId)
        {
            return transaction.IsDposTransaction(chainId);
        }

        // todo: claim fee transaction is a system transaction ?
        public static bool IsClaimFeesTransaction(this Transaction transaction, int chainId)
        {
            return transaction.To.Equals(ContractHelpers.GetTokenContractAddress(chainId)) &&
                   transaction.MethodName.Equals("ClaimTransactionFees");
        }
    }*/
}
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Kernel.Types;

namespace AElf.Kernel
{
    public static class TransactionTypeExtensions
    {
        private static int ChainId { get; } = ChainConfig.Instance.ChainId.ConvertBase58ToChainId();
        private static Address DPosContractAddress { get; } = ContractHelpers.GetConsensusContractAddress(ChainId);

        private static Address TokenContractAddress { get; } = ContractHelpers.GetTokenContractAddress(ChainId);

        public static bool IsDposTransaction(this Kernel.Transaction transaction)
        {
            return transaction.To.Equals(DPosContractAddress);
        }

        public static bool IsSystemTransaction(this Kernel.Transaction transaction)
        {
            return transaction.IsDposTransaction();
        }

        // todo: claim fee transaction is a system transaction ?
        public static bool IsClaimFeesTransaction(this Kernel.Transaction transaction)
        {
            return transaction.To.Equals(TokenContractAddress) && transaction.MethodName.Equals("ClaimTransactionFees");
        }
    }
}
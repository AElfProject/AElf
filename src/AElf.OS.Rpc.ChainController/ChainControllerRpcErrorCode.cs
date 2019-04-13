using System.Collections.Generic;

namespace AElf.OS.Rpc.ChainController
{
    public static class Error
    {
        public const long NotFound = 20001;
        public const long InvalidAddress = 20002;
        public const long InvalidBlockHash = 20003;
        public const long InvalidTransactionId = 20004;
        public const long InvalidProposalId = 20005;
        public const long InvalidOffset = 20006;
        public const long InvalidNum = 20007;
        public const long InvalidTransaction = 20008;
        public const long CannotBroadcastTransaction = 20009;

        public static readonly Dictionary<long, string> Message = new Dictionary<long, string>
        {
            {NotFound, "Not found"},
            {InvalidAddress, "Invalid address format"},
            {InvalidBlockHash, "Invalid block hash format"},
            {InvalidTransactionId, "Invalid Transaction id format"},
            {InvalidProposalId, "Invalid proposal id format"},
            {InvalidOffset, "Offset must greater than or equal to 0"},
            {InvalidNum, "Num must between 0 and 100"},
            {InvalidTransaction, "Invalid transaction information"},
            {CannotBroadcastTransaction, "Sync still in progress, cannot broadcast transactions"}
        };
    }
}
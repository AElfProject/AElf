using System.Collections.Generic;

namespace AElf.WebApp.Application.Chain
{
    public static class Error
    {
        public const int NotFound = 20001;
        public const int InvalidAddress = 20002;
        public const int InvalidBlockHash = 20003;
        public const int InvalidTransactionId = 20004;
        public const int InvalidProposalId = 20005;
        public const int InvalidOffset = 20006;
        public const int InvalidLimit = 20007;
        public const int InvalidTransaction = 20008;
        public const int CannotBroadcastTransaction = 20009;

        public static readonly Dictionary<int, string> Message = new Dictionary<int, string>
        {
            {NotFound, "Not found"},
            {InvalidAddress, "Invalid address format"},
            {InvalidBlockHash, "Invalid block hash format"},
            {InvalidTransactionId, "Invalid Transaction id format"},
            {InvalidProposalId, "Invalid proposal id format"},
            {InvalidOffset, "Offset must greater than or equal to 0"},
            {InvalidLimit, "Limit must between 0 and 100"},
            {InvalidTransaction, "Invalid transaction information"},
            {CannotBroadcastTransaction, "Sync still in progress, cannot broadcast transactions"}
        };
    }
}
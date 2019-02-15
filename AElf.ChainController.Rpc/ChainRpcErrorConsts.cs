using System.Collections.Generic;

namespace AElf.ChainController.Rpc
{
    public static class ChainRpcErrorConsts
    {
        public const long NotFound = 10001;
        public const long InvalidAddress = 10002;
        public const long InvalidBlockHash = 10003;
        public const long InvalidTxId = 10004;
        public const long InvalidProposalId = 10005;
        public const long InvalidOffset = 10006;
        public const long InvalidNum = 10007;
        public const long InvalidTransaction = 10008;
        public const long CannotSendTx = 10009;
        
        public static readonly Dictionary<long, string> RpcErrorMessage = new Dictionary<long, string>
        {
            {NotFound, "Not found"},
            {InvalidAddress, "Invalid address format"},
            {InvalidBlockHash, "Invalid block hash format"},
            {InvalidTxId, "Invalid tx id format"},
            {InvalidProposalId, "Invalid proposal id format"},
            {InvalidOffset, "Offset must greater than or equal to 0"},
            {InvalidNum, "Num must between 0 and 100"},
            {InvalidTransaction, "Invalid transaction information"},
            {CannotSendTx, "Sync still in progress, cannot send transactions"}
        };
    }
}
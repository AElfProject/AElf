namespace AElf.Common
{
    // ReSharper disable InconsistentNaming
    public static class GlobalConfig
    {
        // current release version
        public static int ProtocolVersion = 1;

        public const ulong GenesisBlockHeight = 1;

        public static readonly ulong ReferenceBlockValidPeriod = 64;

        #region data key prefixes

        public const string TransactionTracePrefix = "a";
        public const string BlockBodyPrefix = "b";
        public const string SmartContractPrefix = "c";
        public const string TransactionReceiptPrefix = "e";
        public const string GenesisBlockHashPrefix = "g";
        public const string BlockHeaderPrefix = "h";
        public const string MerkleTreePrefix = "k";
        public const string TransactionResultPrefix = "l";
        public const string FunctionMetadataPrefix = "m";
        public const string ChainHeightPrefix = "n";
        public const string CanonicalPrefix = "o";
        public const string MinersPrefix = "p";
        public const string CurrentBlockHashPrefix = "r";
        public const string CallGraphPrefix = "i";
        public const string StatePrefix = "s";
        public const string TransactionPrefix = "t";

        #endregion data key prefixes
    }
}
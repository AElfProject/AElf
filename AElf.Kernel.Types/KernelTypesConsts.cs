namespace AElf
{
    public static class ContractConsts
    {
        public const ulong GenesisBasicContract = 0;
        public const ulong ConsensusContract = 1;
        public const ulong TokenContract = 2;
        public const ulong CrossChainContract = 3;
        public const ulong AuthorizationContract = 4;
        public const ulong ResourceContract = 5;
        public const ulong DividendsContract = 6;
    }

    public static class ChainConsts
    {
        public const ulong GenesisBlockHeight = 1;
        public const ulong ReferenceBlockValidPeriod = 64;
        public const int ProtocolVersion = 1;
    }

    public static class StorePrefix
    {
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
    }
}
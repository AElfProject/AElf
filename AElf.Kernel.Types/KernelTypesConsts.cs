using AElf.Common;

namespace AElf
{
    //TODO: should move it
    public static class ContractConsts
    {
        public const ulong GenesisBasicContract = 0;
        public const ulong ConsensusContract = 1;
        public const ulong TokenContract = 2;
        public const ulong CrossChainContract = 3;
        public const ulong AuthorizationContract = 4;
        public const ulong ResourceContract = 5;
        public const ulong DividendsContract = 6;
        
        
        public static Hash TokenContractHash = Hash.FromString("AElf.Contracts.Token.TokenContract");
        public static Hash CrossChainHash = Hash.FromString("AElf.Contracts.Token.CrossChainContract");

    }

    public static class ChainConsts
    {
        public const long GenesisBlockHeight = 1;
        public const long ReferenceBlockValidPeriod = 64;
        public const int ProtocolVersion = 1;
    }

    public static class StorePrefix
    {
        public const string TransactionTracePrefix = "a";
        public const string BlockBodyPrefix = "b";
        public const string SmartContractPrefix = "c";
        public const string TransactionReceiptPrefix = "e";
        public const string BlockHeaderPrefix = "h";
        public const string MerkleTreePrefix = "k";
        public const string TransactionResultPrefix = "l";
        public const string FunctionMetadataPrefix = "m";
        public const string MinersPrefix = "p";
        public const string CallGraphPrefix = "i";
        public const string StatePrefix = "s";
        public const string TransactionPrefix = "t";
    }
}
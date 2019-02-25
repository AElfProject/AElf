using AElf.Common;
using AElf.CrossChain;
using AElf.Kernel;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.CrossChain
{
    public class SideChainCreationRequested : Event
    {
        public Address Creator;
        public Hash ChainId;
    }

    public class SideChainCreationRequestApproved : Event
    {
        public SideChainInfo Info;
    }

    public class SideChainDisposal : Event
    {
        public Hash chainId;
    }
    
    public class CrossChainIndexingEvent : Event
    {
        public Hash SideChainTransactionsMerkleTreeRoot;
        public CrossChainBlockData CrossChainBlockData;
    }
}
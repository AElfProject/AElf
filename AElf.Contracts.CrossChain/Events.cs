using AElf.Common;
using AElf.CrossChain;
using AElf.Kernel;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.CrossChain
{
    public class SideChainCreationRequested : Event<SideChainCreationRequested>
    {
        public Address Creator;
        public Hash ChainId;
    }

    public class SideChainCreationRequestApproved : Event<SideChainCreationRequestApproved>
    {
        public SideChainInfo Info;
    }

    public class SideChainDisposal : Event<SideChainDisposal>
    {
        public Hash chainId;
    }
    
    public class CrossChainIndexingEvent : Event<CrossChainIndexingEvent>
    {
        public Hash SideChainTransactionsMerkleTreeRoot;
        public CrossChainBlockData CrossChainBlockData;
    }
}
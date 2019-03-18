using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.CrossChain;
using AElf.Kernel;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.CrossChain
{
    public class SideChainCreationRequested : Event
    {
        public Address Creator { get; set; }
        public int ChainId { get; set; }

        public Miners Miners { get; set; }
    }

    public class SideChainCreationRequestApproved : Event
    {
        public SideChainInfo Info{ get; set; }
    }

    public class SideChainDisposal : Event
    {
        public int ChainId{ get; set; }
    }
    
    public class CrossChainIndexingEvent : Event
    {
        public Hash SideChainTransactionsMerkleTreeRoot{ get; set; }
        public CrossChainBlockData CrossChainBlockData{ get; set; }
        public Address Sender{ get; set; }
    }
}
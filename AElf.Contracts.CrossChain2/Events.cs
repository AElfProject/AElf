using AElf.Common;
using AElf.Kernel;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.CrossChain2
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
}
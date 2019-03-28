using AElf.Sdk.CSharp;

namespace AElf.Contracts.Consensus.DPoS.SideChain
{
    // ReSharper disable once InconsistentNaming
    public class LIBFound : Event
    {
        public long Offset { get; set; }
    }
}
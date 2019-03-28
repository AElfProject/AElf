using AElf.Sdk.CSharp;

namespace AElf.Contracts.Consensus.DPoS
{
    // ReSharper disable once InconsistentNaming
    public class LIBFound : Event
    {
        public long Offset { get; set; }
    }
}
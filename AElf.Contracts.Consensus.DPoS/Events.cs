using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS
{
    // ReSharper disable once InconsistentNaming
    public class LIBFound : Event<LIBFound>
    {
        public ulong Offset { get; set; }
    }
}
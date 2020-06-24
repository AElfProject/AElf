using AElf.Contracts.TestContract.PatchTestContract;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.TestContracts.PatchTestContract
{
    public partial class PatchTestContract : PatchTestContractContainer.PatchTestContractBase
    {
        public const int v = 3;
        
        public override Empty Initialize(Hash input)
        {
            State.Int32State.Value = v;
            return new Empty();
        }
    }
}
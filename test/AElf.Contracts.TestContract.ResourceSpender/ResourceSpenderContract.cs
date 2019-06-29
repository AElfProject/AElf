using Acs8;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestContract.ResourceSpender
{
    public class ResourceSpenderContract : ResourceSpenderContractContainer.ResourceSpenderContractBase
    {
        public override Empty SendForFun(Empty input)
        {
            return new Empty();
        }
    }
}
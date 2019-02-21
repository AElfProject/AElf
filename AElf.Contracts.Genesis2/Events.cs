using AElf.Common;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Genesis
{
    public class ContractHasBeenDeployed : Event
    {
        [Indexed] public Address Creator;
        [Indexed] public Hash CodeHash;
        public Address Address;
    }

    public class OwnerHasBeenChanged : Event
    {
        [Indexed] public Address Address;
        public Address OldOwner;
        public Address NewOwner;
    }
}
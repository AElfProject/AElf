using AElf.Common;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Genesis
{
    public class Events
    {
        public class ContractHasBeenDeployed : Event
        {
            [Indexed] public Address Creator;
            [Indexed] public Address Address;
            [Indexed] public Hash CodeHash;
        }

        public class OwnerHasBeenChanged : Event
        {
            [Indexed] public Address Address;
            [Indexed] public Address OldOwner;
            [Indexed] public Address NewOwner;
        }

    }
}
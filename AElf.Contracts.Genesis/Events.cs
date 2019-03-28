using AElf.Common;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Genesis
{
    public class ContractHasBeenDeployed : Event
    {
        public Address Address;
        [Indexed] public Hash CodeHash;
        [Indexed] public Address Creator;
    }

    public class ContractCodeHasBeenUpdated : Event
    {
        public Address Address;
        public Hash NewCodeHash;
        public Hash OldCodeHash;
    }

    public class OwnerHasBeenChanged : Event
    {
        public Address Address;
        public Address NewOwner;
        public Address OldOwner;
    }
}
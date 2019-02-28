using AElf.Common;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Genesis
{
    public class ContractHasBeenDeployed : Event<ContractHasBeenDeployed>
    {
        [Indexed] public Address Creator;
        [Indexed] public Hash CodeHash;
        public Address Address;
    }

    public class ContractCodeHasBeenUpdated : Event<ContractCodeHasBeenUpdated>
    {
        public Address Address;
        public Hash OldCodeHash;
        public Hash NewCodeHash;
    }

    public class OwnerHasBeenChanged : Event<OwnerHasBeenChanged>
    {
        public Address Address;
        public Address OldOwner;
        public Address NewOwner;
    }
}
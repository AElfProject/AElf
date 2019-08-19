using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Configuration
{
    public partial class ConfigurationContract : ConfigurationContainer.ConfigurationBase
    {
        public override Empty SetBlockTransactionLimit(Int32Value input)
        {
            CheckOwnerAuthority();
            
            var oldValue = State.BlockTransactionLimit.Value;
            var newValue = input.Value;
            State.BlockTransactionLimit.Value = newValue;
            Context.Fire(new BlockTransactionLimitChanged
            {
                Old = oldValue,
                New = newValue
            });
            return new Empty();
        }

        public override Int32Value GetBlockTransactionLimit(Empty input)
        {
            return new Int32Value {Value = State.BlockTransactionLimit.Value};
        }

        public override Empty SetTransactionOwnerAddress(Address input)
        {
            CheckOwnerAuthority();
            State.Owner.Value = input;
            return new Empty();
        }

        public override Address GetTransactionOwnerAddress(Empty input)
        {
            var address = GetOwnerAddress();
            return address;
        }
    }
}
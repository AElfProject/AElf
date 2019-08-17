using AElf.Sdk.CSharp;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Configuration
{
    public class ConfigurationContract : ConfigurationContainer.ConfigurationBase
    {
        public override Empty SetBlockTransactionLimit(Int32Value input)
        {
            // TODO: Check permission
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
    }
}
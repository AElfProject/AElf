using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Configuration
{
    public partial class ConfigurationContract : ConfigurationContainer.ConfigurationBase
    {
        public override Empty SetBlockTransactionLimit(Int32Value input)
        {
            Assert(input.Value > 0, "Invalid input.");
            CheckControllerAuthority();

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

        public override Empty ChangeConfigurationController(Address input)
        {
            CheckControllerAuthority();
            State.ConfigurationController.Value = input;
            return new Empty();
        }

        public override Address GetConfigurationController(Empty input)
        {
            var address = GetControllerForManageConfiguration();
            return address;
        }

        public override Empty SetRequiredAcsInContracts(RequiredAcsInContracts input)
        {
            CheckSenderIsControllerOrZeroContract();
            State.RequiredAcsInContracts.Value = input;
            return new Empty();
        }

        public override RequiredAcsInContracts GetRequiredAcsInContracts(Empty input)
        {
            return State.RequiredAcsInContracts.Value ?? new RequiredAcsInContracts();
        }
    }
}
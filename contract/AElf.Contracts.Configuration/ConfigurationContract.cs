using System.Linq;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Configuration
{
    public partial class ConfigurationContract : ConfigurationContainer.ConfigurationBase
    {
        public override Empty SetConfiguration(SetConfigurationInput input)
        {
            CheckSenderIsControllerOrZeroContract();
            Assert(input.Key.Any() && input.Value != null, "Invalid set config input.");
            State.Configurations[input.Key] = new BytesValue {Value = input.Value};
            Context.Fire(new ConfigurationSet
            {
                Key = input.Key,
                Value = input.Value
            });
            return new Empty();
        }

        public override BytesValue GetConfiguration(StringValue input)
        {
            var value = State.Configurations[input.Value];
            return value ?? new BytesValue();
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
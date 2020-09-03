using System.Linq;
using AElf.Standards.ACS1;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Configuration
{
    public partial class ConfigurationContract : ConfigurationImplContainer.ConfigurationImplBase
    {
        public override Empty SetConfiguration(SetConfigurationInput input)
        {
            AssertPerformedByConfigurationControllerOrZeroContract();
            Assert(input.Key.Any() && input.Value != ByteString.Empty, "Invalid set config input.");
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

        public override Empty ChangeConfigurationController(AuthorityInfo input)
        {
            AssertPerformedByConfigurationController();
            Assert(input != null, "invalid input");
            Assert(CheckOrganizationExist(input),"Invalid authority input.");
            State.ConfigurationController.Value = input;
            return new Empty();
        }

        public override AuthorityInfo GetConfigurationController(Empty input)
        {
            return State.ConfigurationController.Value ?? GetDefaultConfigurationController();
        }
    }
}
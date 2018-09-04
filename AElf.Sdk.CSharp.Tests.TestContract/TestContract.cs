using AElf.Sdk.CSharp.Types;
using AElf.Kernel;
using AElf.Types.CSharp;
using AElf.Types.CSharp.MetadataAttribute;
using Google.Protobuf;


namespace AElf.Sdk.CSharp.Tests
{
    public class Account : UserType
    {
        public string Name;
        public Hash Address { get; set; }
    }

    public class AccountName : Event
    {
        public string Name;
    }

    public class TestContract : CSharpSmartContract
    {
        [SmartContractFieldData("${this}._stopped", DataAccessMode.ReadWriteAccountSharing)]
        private BoolField _stopped = new BoolField("_stopped");
        
        [SmartContractFieldData("${this}._account", DataAccessMode.ReadWriteAccountSharing)]
        private UserTypeField<Account> _account = new UserTypeField<Account>("_account");

        [SmartContractFunction("${this}.GetTotalSupply", new string[]{}, new string[]{})]
        public uint GetTotalSupply()
        {
            return 100;
        }

        [SmartContractFunction("${this}.SetAccount", new string[]{}, new []{"${this}._account"})]
        public bool SetAccount(string name, Hash address)
        {
            var account = new Account()
            {
                Name = name,
                Address = address
            };
            // this is used for testing UserTypeField
            _account.SetValue(account);
            return true;
        }

        
        [SmartContractFunction("${this}.GetAccountName", new string[]{}, new []{"${this}._account"})]
        public string GetAccountName()
        {
            var account = _account.GetValue();
            new AccountName()
            {
                Name = account.Name
            }.Fire();
            return account.Name;
        }

        public void InlineCallToZero()
        {
            Api.SendInline(Hash.Zero, "Dummy");
        }
    }
}

using System;
using System.Threading.Tasks;
using AElf.Sdk.CSharp.Types;
using AElf.Kernel;
using AElf.Kernel.Concurrency.Metadata;
using AElf.Sdk.CSharp;
using AElf.Types.CSharp;


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
        public async Task<bool> SetAccount(string name, Hash address)
        {
            var account = new Account()
            {
                Name = name,
                Address = address
            };
            // this is used for testing UserTypeField
            await _account.SetAsync(account);
            return true;
        }

        
        [SmartContractFunction("${this}.GetAccountName", new string[]{}, new []{"${this}._account"})]
        public async Task<string> GetAccountName()
        {
            var account = await _account.GetAsync();
            new AccountName()
            {
                Name = account.Name
            }.Fire();
            return account.Name;
        }
    }
}

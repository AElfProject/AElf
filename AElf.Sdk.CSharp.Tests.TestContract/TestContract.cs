using System.Threading.Tasks;
using AElf.Sdk.CSharp.Types;
using AElf.Kernel;
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
        private BoolField _stopped = new BoolField("_stopped");
        private UserTypeField<Account> _account = new UserTypeField<Account>("_account");

        public override async Task InvokeAsync()
        {
            // this is not needed anymore, put here as placeholder
            // before we remove this from interface
            await Task.CompletedTask;
        }

        public uint GetTotalSupply()
        {
            return 100;
        }

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

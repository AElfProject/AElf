using System;
using System.Threading.Tasks;
using AElf.Sdk.CSharp.Types;
using AElf.Kernel;
using AElf.Kernel.Types;
using AElf.Sdk.CSharp;
using AElf.Types.CSharp;

namespace AElf.ABI.CSharp.Tests
{
    public class Account : UserType
    {
        public string Name;
        public Hash Address { get; set; }
    }

    public class AccountName : AElf.Sdk.CSharp.Event
    {
        public string Name;
    }

    public class UserContract : UserBaseContract
    {
        private BoolField _stopped = new BoolField("_stopped");
        private UserTypeField<Account> _account = new UserTypeField<Account>("_account");

        public async Task<bool> SetAccount(string name, Hash address)
        {
            var account = new Account()
            {
                Name = name,
                Address = address
            };
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

        private void PrivateMethodNotExposed()
        {

        }
    }
}

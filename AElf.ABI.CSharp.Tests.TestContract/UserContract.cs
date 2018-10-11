using System;
using System.Threading.Tasks;
using AElf.Sdk.CSharp.Types;
using AElf.Kernel;
using AElf.Kernel.Types;
using AElf.Sdk.CSharp;
using AElf.Types.CSharp;
using AElf.Common;

namespace AElf.ABI.CSharp.Tests
{
    public class Account : UserType
    {
        public string Name;
        public Address Address { get; set; }
    }

    public class AccountName : AElf.Sdk.CSharp.Event
    {
        [Indexed]
        public string Name;

        public string Dummy;
    }

    public class UserContract : UserBaseContract
    {
        private BoolField _stopped = new BoolField("_stopped");
        private UserTypeField<Account> _account = new UserTypeField<Account>("_account");

        public async Task<bool> SetAccount(string name, Address address)
        {
            var account = new Account()
            {
                Name = name,
                Address = address
            };
            await _account.SetAsync(account);
            return true;
        }

        [View]
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

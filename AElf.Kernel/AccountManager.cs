using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    class AccountManager : IAccountManager
    {
        public List<IAccount> ExecuteTransactionAsync(ITransaction tx)
        {
            var accountFrom = tx.AccountFrom;
            var accountTo = tx.AccountFrom;
            var amount = tx.Amount;

            var stateFrom = WorldState.GetAmountByAccount(accountFrom);
            var stateTo = WorldState.GetAmountByAccount(accountTo);

            accountFrom.Amount = stateFrom - amount;
            accountTo.Amount = stateFrom + amount;

            var list = new List<IAccount>() { accountFrom, accountTo };

            return list;
        }
    }
}
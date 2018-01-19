using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    class AccountManager : IAccountManager
    {
        public List<Account> ExecuteTransactionAsync(ITransaction tx)
        {
            var accountFrom = ((tx as Transaction).AccountFrom as Account);
            var accountTo = ((tx as Transaction).AccountFrom as Account);
            var amount = (tx as Transaction).Amount;

            var stateFrom = WorldState.GetAmountByAccount(accountFrom);
            var stateTo = WorldState.GetAmountByAccount(accountTo);

            accountFrom.Amount = stateFrom - amount;
            accountTo.Amount = stateFrom + amount;

            var list = new List<Account>() { accountFrom, accountTo };

            return list;
        }
    }
}
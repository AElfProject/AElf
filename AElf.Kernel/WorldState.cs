using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public class WorldState : IWorldState
    {
        public static Dictionary<IAccount, int> _dictAcountAmount = new Dictionary<IAccount, int>();

        public static int GetAmountByAccount(IAccount account)
        {
            return _dictAcountAmount[account];
        }

        public IAccountDataProvider GetAccountDataProviderByAccountAsync(IAccount account)
        {
            throw new NotImplementedException();
        }
    }
}

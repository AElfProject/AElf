using System.Threading.Tasks;
using AElf.Kernel.Extensions;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel
{
    public class AccountManager : IAccountManager
    {
        private WorldState _worldState;

        public AccountManager(WorldState worldState)
        {
            _worldState = worldState;
        }

        public Task<IAccount> CreateAccount(byte[] smartContract)
        {
            throw new System.NotImplementedException();
        }

        public IAccount GetAccountByHash(IHash<IAccount> hash)
        {
            throw new System.NotImplementedException();
        }

    }
}

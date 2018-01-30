using System.Threading.Tasks;

namespace AElf.Kernel
{
    
    public interface ISmartContract
    {
        void SetAccountDataProvider(IAccountDataProvider provider);
        Task Invoke(IHash<IAccount> caller, string methodname, params object[] objs);
    }
}
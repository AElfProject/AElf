using System.Dynamic;
using System.Threading.Tasks;

namespace AElf.Cryptography
{
    public interface IKeyStore
    {
        bool IsOpen { get; }

        Task OpenAsync(string password);
    }
}
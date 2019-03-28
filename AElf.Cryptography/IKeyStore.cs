using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;

namespace AElf.Cryptography
{
    public interface IKeyStore
    {
        bool IsOpen { get; }

        Task<AElfKeyStore.Errors> OpenAsync(string address, string password, bool withTimeout = true);

        ECKeyPair GetAccountKeyPair(string address);

        Task<ECKeyPair> CreateAsync(string password, string chainId);

        Task<List<string>> ListAccountsAsync();

        Task<ECKeyPair> ReadKeyPairAsync(string address, string password);
    }
}
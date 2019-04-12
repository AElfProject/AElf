using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;

namespace AElf.Cryptography
{
    public interface IKeyStore
    {
        Task<AElfKeyStore.Errors> OpenAsync(string address, string password, bool withTimeout = true);

        ECKeyPair GetAccountKeyPair(string address);

        Task<ECKeyPair> CreateAsync(string password, string chainId);

        Task<List<string>> ListAccountsAsync();

        Task<ECKeyPair> ReadKeyPairAsync(string address, string password);
    }
}
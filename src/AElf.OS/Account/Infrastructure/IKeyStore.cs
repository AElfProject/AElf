using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;

namespace AElf.OS.Account.Infrastructure
{
    public interface IKeyStore
    {
        Task<AElfKeyStore.Errors> OpenAsync(string address, string password, bool withTimeout = true);

        ECKeyPair GetAccountKeyPair(string address);

        Task<ECKeyPair> CreateAsync(string password);

        Task<List<string>> ListAccountsAsync();
    }
}
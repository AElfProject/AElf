using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Common.Application;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.OS;
using AElf.OS.Rpc;
using Anemonis.AspNetCore.JsonRpc;
using Microsoft.Extensions.Options;

namespace AElf.OS.Rpc.Wallet
{
    [Path("/wallet")]
    public class WalletRpcService : IJsonRpcService
    {
        private AElfKeyStore _ks;

        private AElfKeyStore KeyStore =>
            _ks ?? (_ks = new AElfKeyStore(Path.Combine(ApplicationHelper.AppDataPath, "rpc-managed-wallet")));

        [JsonRpcMethod("ListAccounts")]
        public async Task<List<string>> ListAccounts()
        {
            return await KeyStore.ListAccountsAsync();
        }

        [JsonRpcMethod("SignHash", "address", "password", "hash")]
        public async Task<string> SignHash(string address, string password, string hash)
        {
            var tryOpen = await KeyStore.OpenAsync(address, password);
            if (tryOpen == AElfKeyStore.Errors.WrongPassword)
            {
                throw new JsonRpcServiceException(Error.WrongPassword, Error.Message[Error.WrongPassword]);
            }

            var kp = KeyStore.GetAccountKeyPair(address);
            if (kp == null)
            {
                throw new JsonRpcServiceException(Error.AccountNotExist, Error.Message[Error.AccountNotExist]);
            }

            var toSig = ByteArrayHelpers.FromHexString(hash);
            var signature = CryptoHelpers.SignWithPrivateKey(kp.PrivateKey, toSig);
            return signature.ToHex();
        }
    }
}
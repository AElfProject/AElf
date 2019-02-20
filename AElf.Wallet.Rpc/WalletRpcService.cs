using System.IO;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Common.Application;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.RPC;
using Anemonis.AspNetCore.JsonRpc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace AElf.Wallet.Rpc
{
    [Path("/wallet")]
    public class WalletRpcService : IJsonRpcService
    {
        private AElfKeyStore _ks;

        private AElfKeyStore KeyStore =>
            _ks ?? (_ks = new AElfKeyStore(Path.Combine(ApplicationHelpers.ConfigPath, "rpc-managed-wallet")));

        private readonly ChainOptions _chainOptions;

        public WalletRpcService(IOptionsSnapshot<ChainOptions> options)
        {
            _chainOptions = options.Value;
        }

        [JsonRpcMethod("ListAccounts")]
        public async Task<JObject> ListAccounts()
        {
            var accounts = await KeyStore.ListAccountsAsync();
            return JObject.FromObject(accounts);
        }

        [JsonRpcMethod("SignHash", "address", "password", "hash")]
        public async Task<JObject> SignHash(string address, string password, string hash)
        {
            var tryOpen = await KeyStore.OpenAsync(address, password);
            if (tryOpen == AElfKeyStore.Errors.WrongPassword)
            {
                throw new JsonRpcServiceException(WalletRpcErrorConsts.WrongPassword,
                    WalletRpcErrorConsts.RpcErrorMessage[WalletRpcErrorConsts.WrongPassword]);
            }

            var kp = KeyStore.GetAccountKeyPair(address);
            if (kp == null)
            {
                throw new JsonRpcServiceException(WalletRpcErrorConsts.AccountNotExist,
                    WalletRpcErrorConsts.RpcErrorMessage[WalletRpcErrorConsts.AccountNotExist]);
            }

            var toSig = ByteArrayHelpers.FromHexString(hash);
            // Sign the hash
            var signature = CryptoHelpers.SignWithPrivateKey(kp.PrivateKey, toSig);

            // TODO: Standardize encoding
            // todo test
            return new JObject
            {
                ["Signature"] = signature
            };
        }
    }
}
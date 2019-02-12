using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Common.Application;
using AElf.Configuration;
using AElf.Configuration.Config.Chain;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.RPC;
using Community.AspNetCore.JsonRpc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace AElf.Wallet.Rpc
{
    [Path("/wallet")]
    public class WalletRpcService : IJsonRpcService
    {
        private AElfKeyStore _ks;

        private AElfKeyStore KeyStore
        {
            get
            {
                if (_ks == null)
                {
                    _ks = new AElfKeyStore(
                        Path.Combine(ApplicationHelpers.ConfigPath, "rpc-managed-wallet")
                    );
                }

                return _ks;
            }
        }
        
        private readonly ChainOptions _chainOptions;
        
        public WalletRpcService(IOptionsSnapshot<ChainOptions> options)
        {
            _chainOptions = options.Value;
        }

        #region Methods

        [JsonRpcMethod("create_account", "password")]
        public async Task<JObject> CreateAccount(string password)
        {
            try
            {
                var keypair = await KeyStore.CreateAsync(password, _chainOptions.ChainId);
                if (keypair != null)
                {
                    // todo warning return null for now
                    return new JObject();
                }

                return new JObject
                {
                    ["address"] = "0x",
                    ["error"] = "Failed"
                };
            }
            catch (Exception e)
            {
                return new JObject
                {
                    ["error"] = e.ToString()
                };
            }
        }

        [JsonRpcMethod("list_accounts")]
        public async Task<List<string>> ListAccounts()
        {
            return await KeyStore.ListAccountsAsync();
        }

        [JsonRpcMethod("sign_hash", "address", "password", "hash")]
        public async Task<JObject> SignHash(string address, string password, string hash)
        {
            var tryOpen = await KeyStore.OpenAsync(address, password);
            if (tryOpen == AElfKeyStore.Errors.WrongPassword)
            {
                return new JObject
                {
                    ["error"] = "Wrong password."
                };
            }

            var kp = KeyStore.GetAccountKeyPair(address);

            var toSig = ByteArrayHelpers.FromHexString(hash);
            // Sign the hash
            var signature = CryptoHelpers.SignWithPrivateKey(kp.PrivateKey, toSig);

            // TODO: Standardize encoding
            // todo test
            return new JObject
            {
                ["sig"] = signature
            };
        }

        #endregion
    }
}
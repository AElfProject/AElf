using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Common.Application;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
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

        [JsonRpcMethod("CreateAccount", "password")]
        public async Task<JObject> CreateAccount(string password)
        {
            ECKeyPair keypair;
            try
            {
                keypair = await KeyStore.CreateAsync(password, _chainOptions.ChainId);
            }
            catch(Exception e)
            {
                throw new JsonRpcServiceException(WalletRpcErrorConsts.CreateAccountFailed,
                    WalletRpcErrorConsts.RpcErrorMessage[WalletRpcErrorConsts.CreateAccountFailed], e);
            }

            var createResult = keypair != null;
            return new JObject(createResult);
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

        #endregion
    }
}
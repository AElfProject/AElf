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
using AElf.RPC;
using Base58Check;
using Community.AspNetCore.JsonRpc;
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
                        Path.Combine(ApplicationHelpers.GetDefaultConfigPath(), "rpc-managed-wallet")
                    );
                }

                return _ks;
            }
        }

        #region Methods

        [JsonRpcMethod("create_account", "password")]
        public async Task<JObject> CreateAccount(string password)
        {
            try
            {
                var chainPrefixBase58 =
                    Base58CheckEncoding.EncodePlain(ByteArrayHelpers.FromHexString(ChainConfig.Instance.ChainId));

                var chainPrefix = chainPrefixBase58.Substring(0, 4);
                
                var keypair = KeyStore.Create(password, chainPrefix);
                if (keypair != null)
                {
                    // todo warning return null for now
                    return new JObject
                    {
                        //["address"] = keypair.FormattedAddress()
                    };
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
            return KeyStore.ListAccounts();
        }

        [JsonRpcMethod("sign_hash", "address", "password", "hash")]
        public async Task<JObject> SignHash(string address, string password, string hash)
        {
            var tryOpen = KeyStore.OpenAsync(address, password);
            if (tryOpen == AElfKeyStore.Errors.WrongPassword)
            {
                return new JObject()
                {
                    ["error"] = "Wrong password."
                };
            }

            ECKeyPair kp = KeyStore.GetAccountKeyPair(address);

            byte[] toSig = ByteArrayHelpers.FromHexString(hash);
            // Sign the hash
            ECSigner signer = new ECSigner();
            ECSignature signature = signer.Sign(kp, toSig);

            // TODO: Standardize encoding
            // todo test
            return new JObject()
            {
                ["sig"] = signature.SigBytes
            };
        }

        #endregion
    }
}
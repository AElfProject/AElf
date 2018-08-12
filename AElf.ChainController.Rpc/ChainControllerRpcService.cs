using System;
using System.Threading.Tasks;
using AElf.Common.ByteArrayHelpers;
using AElf.Configuration;
using Community.AspNetCore.JsonRpc;
using Newtonsoft.Json.Linq;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.RPC;
using AElf.SmartContract;
using AsyncEventAggregator;
using Google.Protobuf;

namespace AElf.ChainController.Rpc
{
    [Path("")]
    public class ChainControllerRpcService : IJsonRpcService
    {
        #region Properties

        public INodeConfig NodeConfig { get; set; }
        public IChainService ChainService { get; set; }
        public IChainCreationService ChainCreationService { get; set; }
        public ITxPoolService TxPoolService { get; set; }
        public ITransactionManager TransactionManager { get; set; }
        public ITransactionResultService TransactionResultService { get; set; }
        public ISmartContractService SmartContractService { get; set; }

        #endregion Properties


        #region Methods

        [JsonRpcMethod("connect_chain")]
        public Task<JObject> ProGetChainInfo()
        {
            Console.WriteLine("connect_chain");
            try
            {
                var chainId = NodeConfig.ChainId;
                var basicContractZero = this.GetGenesisContractHash(SmartContractType.BasicContractZero);
                var tokenContract = this.GetGenesisContractHash(SmartContractType.TokenContract);
                var response = new JObject()
                {
                    ["result"] =
                        new JObject
                        {
                            [SmartContractType.BasicContractZero.ToString()] = basicContractZero.ToHex(),
                            [SmartContractType.TokenContract.ToString()] = tokenContract.ToHex(),
                            ["chain_id"] = chainId.ToHex()
                        }
                };

                return Task.FromResult(JObject.FromObject(response));
            }
            catch (Exception e)
            {
                var response = new JObject
                {
                    ["exception"] = e.ToString()
                };

                return Task.FromResult(JObject.FromObject(response));
            }
        }

        [JsonRpcMethod("get_contract_abi", "address")]
        public async Task<JObject> ProcessGetContractAbi(string address)
        {
            try
            {
                var addrHash = new Hash
                {
                    Value = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(address))
                };

                var abi = await this.GetContractAbi(addrHash);

                return new JObject
                {
                    ["address"] = address,
                    ["abi"] = abi.ToByteArray().ToHex(),
                    ["error"] = ""
                };
            }
            catch (Exception e)
            {
                return new JObject
                {
                    ["address"] = address,
                    ["abi"] = "",
                    ["error"] = "Not Found"
                };
            }
        }

        [JsonRpcMethod("broadcast_tx", "rawtx")]
        public async Task<JObject> ProcessBroadcastTx(string raw64)
        {
            var hexString = ByteArrayHelpers.FromHexString(raw64);
            var transaction = Transaction.Parser.ParseFrom(hexString);

            // TODO: Wrap Transaction into a message
            await this.Publish(((ITransaction)transaction).AsTask());

            var res = new JObject {["hash"] = transaction.GetHash().ToHex()};
            return await Task.FromResult(res);
        }

        [JsonRpcMethod("get_tx_result", "txhash")]
        public async Task<JObject> ProcGetTxResult(string txhash)
        {
            Hash txHash;
            try
            {
                txHash = ByteArrayHelpers.FromHexString(txhash);
            }
            catch (Exception e)
            {
                return JObject.FromObject(new JObject
                {
                    ["error"] = "Invalid Address Format"
                });
            }

            try
            {
                var transaction = await this.GetTransaction(txHash);

                var txInfo = transaction == null
                    ? new JObject {["tx"] = "Not Found"}
                    : transaction.GetTransactionInfo();
                if (transaction != null)
                    ((JObject) txInfo["tx"]).Add("params",
                        String.Join(", ", await this.GetTransactionParameters(transaction)));

                var txResult = await this.GetTransactionResult(txHash);
                var response = new JObject
                {
                    ["tx_status"] = txResult.Status.ToString(),
                    ["tx_info"] = txInfo["tx"]
                };

                if (txResult.Status == Status.Failed)
                {
                    response["tx_error"] = txResult.RetVal.ToStringUtf8();
                }

                if (txResult.Status == Status.Mined)
                {
                    response["return"] = txResult.RetVal.ToByteArray().ToHex();
                }
                // Todo: it should be deserialized to obj ion cli, 

                return JObject.FromObject(new JObject {["result"] = response});
            }
            catch (Exception e)
            {
                return new JObject
                {
                    ["error"] = e.ToString()
                };
            }
        }

        #endregion Methods
    }
}
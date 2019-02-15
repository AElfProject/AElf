using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ChainController.CrossChain;
using AElf.ChainController.EventMessages;
using AElf.Kernel;
using AElf.Common;
using AElf.Kernel.Managers;
using AElf.Kernel.Storages;
using AElf.Kernel.Types;
using AElf.Miner.TxMemPool;
using AElf.Node.AElfChain;
using AElf.RPC;
using AElf.SmartContract;
using AElf.SmartContract.Consensus;
using AElf.SmartContract.Proposal;
using AElf.Synchronization.BlockSynchronization;
using Anemonis.AspNetCore.JsonRpc;
using Easy.MessageHub;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Transaction = AElf.Kernel.Transaction;

namespace AElf.ChainController.Rpc
{
    [Path("/chain")]
    public class ChainControllerRpcService : IJsonRpcService
    {
        #region Properties

        public IChainService ChainService { get; set; }
        public IChainContextService ChainContextService { get; set; }
        public IChainCreationService ChainCreationService { get; set; }
        public ITxHub TxHub { get; set; }
        public ITransactionResultService TransactionResultService { get; set; }
        public ITransactionTraceManager TransactionTraceManager { get; set; }
        public ISmartContractService SmartContractService { get; set; }
        public INodeService MainchainNodeService { get; set; }
        public ICrossChainInfoReader CrossChainInfoReader { get; set; }
        public IAuthorizationInfoReader AuthorizationInfoReader { get; set; }
        public IBlockSynchronizer BlockSynchronizer { get; set; }
        public IBinaryMerkleTreeManager BinaryMerkleTreeManager { get; set; }
        public IElectionInfo ElectionInfo { get; set; }

        public IStateStore<BlockStateSet> BlockStateSets { get; set; }

        #endregion Properties

        public ILogger<ChainControllerRpcService> Logger { get; set; }

        private readonly ChainOptions _chainOptions;

        private bool _canBroadcastTxs = true;
        private readonly int _chainId;
        public ChainControllerRpcService(IOptionsSnapshot<ChainOptions> options)
        {
            Logger = NullLogger<ChainControllerRpcService>.Instance;
            _chainOptions = options.Value;
            _chainId = _chainOptions.ChainId.ConvertBase58ToChainId();

            MessageHub.Instance.Subscribe<ReceivingHistoryBlocksChanged>(msg => _canBroadcastTxs = !msg.IsReceiving);
        }

        #region Methods

        [JsonRpcMethod("GetCommands")]
        public async Task<JObject> GetCommands()
        {
            var methodContracts = this.GetRpcMethodContracts();
            var commands = methodContracts.Keys.OrderBy(x => x).ToList();
            var json = JsonConvert.SerializeObject(commands);
            var arrCommands = JArray.Parse(json);
            var response = new JObject
            {
                ["Commands"] = arrCommands
            };
            return await Task.FromResult(JObject.FromObject(response));
        }

        [JsonRpcMethod("GetChainInfo")]
        public async Task<JObject> GetChainInfo()
        {
            var basicContractZero =
                ContractHelpers.GetGenesisBasicContractAddress(_chainId);
            var crosschainContract =
                ContractHelpers.GetCrossChainContractAddress(_chainId);
            var authorizationContract =
                ContractHelpers.GetAuthorizationContractAddress(_chainId);
            var tokenContract = ContractHelpers.GetTokenContractAddress(_chainId);
            var consensusContract = ContractHelpers.GetConsensusContractAddress(_chainId);
            var dividendsContract = ContractHelpers.GetDividendsContractAddress(_chainId);

            //var tokenContract = this.GetGenesisContractHash(SmartContractType.TokenContract);
            var response = new JObject
            {
                [GlobalConfig.GenesisSmartContractZeroAssemblyName] = basicContractZero.GetFormatted(),
                [GlobalConfig.GenesisCrossChainContractAssemblyName] = crosschainContract.GetFormatted(),
                [GlobalConfig.GenesisAuthorizationContractAssemblyName] =
                    authorizationContract.GetFormatted(),
                [GlobalConfig.GenesisTokenContractAssemblyName] = tokenContract.GetFormatted(),
                [GlobalConfig.GenesisConsensusContractAssemblyName] = consensusContract.GetFormatted(),
                [GlobalConfig.GenesisDividendsContractAssemblyName] = dividendsContract.GetFormatted(),
                ["ChainId"] = _chainOptions.ChainId
            };

            return response;
        }

        [JsonRpcMethod("GetContractAbi", "address")]
        public async Task<JObject> GetContractAbi(string address)
        {
            Address addressHash;
            try
            {
                addressHash = Address.Parse(address);
            }
            catch
            {
                throw new JsonRpcServiceException(ChainRpcErrorConsts.InvalidAddress,
                    ChainRpcErrorConsts.RpcErrorMessage[ChainRpcErrorConsts.InvalidAddress]);
            }
            var abi = await this.GetContractAbi(_chainId, addressHash);

            if (abi == null)
            {
                throw new JsonRpcServiceException(ChainRpcErrorConsts.NotFound,
                    ChainRpcErrorConsts.RpcErrorMessage[ChainRpcErrorConsts.NotFound]);
            }

            return new JObject
            {
                ["Address"] = address,
                ["Abi"] = abi.ToByteArray().ToHex()
            };
        }

        [JsonRpcMethod("Call", "rawTx")]
        public async Task<JObject> CallReadOnly(string rawTx)
        {
            byte[] response;
            try
            {
                var hexString = ByteArrayHelpers.FromHexString(rawTx);
                var transaction = Transaction.Parser.ParseFrom(hexString);
                response = await this.CallReadOnly(_chainId, transaction);
            }
            catch
            {
                throw new JsonRpcServiceException(ChainRpcErrorConsts.InvalidTransaction,
                    ChainRpcErrorConsts.RpcErrorMessage[ChainRpcErrorConsts.InvalidTransaction]);
            }

            return new JObject
            {
                ["Return"] = response?.ToHex()
            };
        }

        [JsonRpcMethod("BroadcastTx", "rawTx")]
        public async Task<JObject> BroadcastTx(string rawTx)
        {
            if (!_canBroadcastTxs)
            {
                throw new JsonRpcServiceException(ChainRpcErrorConsts.CannotSendTx,
                    ChainRpcErrorConsts.RpcErrorMessage[ChainRpcErrorConsts.CannotSendTx]);
            }

            Transaction transaction;
            try
            {
                var hexString = ByteArrayHelpers.FromHexString(rawTx);
                transaction = Transaction.Parser.ParseFrom(hexString);
            }
            catch
            {
                throw new JsonRpcServiceException(ChainRpcErrorConsts.InvalidTransaction,
                    ChainRpcErrorConsts.RpcErrorMessage[ChainRpcErrorConsts.InvalidTransaction]);
            }

            var response = new JObject {["Hash"] = transaction.GetHash().ToHex()};

            //TODO: Wait validation done
            transaction.GetTransactionInfo();
            await TxHub.AddTransactionAsync(_chainId, transaction);

            return response;
        }

        [JsonRpcMethod("BroadcastTxs", "rawTxs")]
        public async Task<JObject> BroadcastTxs(string rawTxs)
        {
            if (!_canBroadcastTxs)
            {
                throw new JsonRpcServiceException(ChainRpcErrorConsts.CannotSendTx,
                    ChainRpcErrorConsts.RpcErrorMessage[ChainRpcErrorConsts.CannotSendTx]);
            }

            var response = new List<object>();

            foreach (var rawTx in rawTxs.Split(','))
            {
                JObject result;
                try
                {
                    result = await BroadcastTx(rawTx);
                }
                catch
                {
                    break;
                }

                response.Add(result["Hash"].ToString());
            }

            return new JObject
            {
                JToken.FromObject(response)
            };
        }

        [JsonRpcMethod("GetTxMerklePath", "txId")]
        public async Task<JObject> GetTxMerklePath(string txId)
        {
            Hash txHash;
            try
            {
                txHash = Hash.LoadHex(txId);
            }
            catch
            {
                throw new JsonRpcServiceException(ChainRpcErrorConsts.InvalidTxId,
                    ChainRpcErrorConsts.RpcErrorMessage[ChainRpcErrorConsts.InvalidTxId]);
            }

            var txResult = await this.GetTransactionResult(txHash);
            if (txResult == null)
            {
                throw new JsonRpcServiceException(ChainRpcErrorConsts.NotFound,
                    ChainRpcErrorConsts.RpcErrorMessage[ChainRpcErrorConsts.NotFound]);
            }

            var binaryMerkleTree = await this.GetBinaryMerkleTreeByHeight(_chainId, txResult.BlockNumber);
            var merklePath = binaryMerkleTree.GenerateMerklePath(txResult.Index);
            if (merklePath == null)
            {
                throw new JsonRpcServiceException(ChainRpcErrorConsts.NotFound,
                    ChainRpcErrorConsts.RpcErrorMessage[ChainRpcErrorConsts.NotFound]);
            }

            MerklePath merklePathInParentChain = null;
            ulong boundParentChainHeight = 0;
            try
            {
                merklePathInParentChain = await this.GetTxRootMerklePathInParentChain(_chainId, txResult.BlockNumber);
                boundParentChainHeight = await this.GetBoundParentChainHeight(_chainId, txResult.BlockNumber);
            }
            catch (Exception e)
            {
                throw new JsonRpcServiceException(ChainRpcErrorConsts.NotFound,
                    ChainRpcErrorConsts.RpcErrorMessage[ChainRpcErrorConsts.NotFound], e);
            }

            if (merklePathInParentChain != null)
                merklePath.Path.AddRange(merklePathInParentChain.Path);
            return new JObject
            {
                ["MerklePath"] = merklePath.ToByteArray().ToHex(),
                ["ParentHeight"] = boundParentChainHeight
            };
        }

        [JsonRpcMethod("GetParentChainBlockInfo", "height")]
        public async Task<JObject> GetParentChainBlockInfo(ulong height)
        {
            var merklePathInParentChain = await this.GetParentChainBlockInfo(_chainId, height);
            if (merklePathInParentChain == null)
            {
                throw new JsonRpcServiceException(ChainRpcErrorConsts.NotFound,
                    ChainRpcErrorConsts.RpcErrorMessage[ChainRpcErrorConsts.NotFound]);
            }

            return new JObject
            {
                ["ParentChainId"] = merklePathInParentChain.Root.ChainId.DumpBase58(),
                ["SideChainTxsRoot"] = merklePathInParentChain.Root.SideChainTransactionsRoot.ToHex(),
                ["ParentHeight"] = merklePathInParentChain.Height
            };
        }

        [JsonRpcMethod("GetTxResult", "txId")]
        public async Task<JObject> GetTxResult(string txId)
        {
            Hash txHash;
            try
            {
                txHash = Hash.LoadHex(txId);
            }
            catch
            {
                throw new JsonRpcServiceException(ChainRpcErrorConsts.InvalidTxId,
                    ChainRpcErrorConsts.RpcErrorMessage[ChainRpcErrorConsts.InvalidTxId]);
            }

            var response = await GetTx(txHash);
            return response;
        }

        [JsonRpcMethod("GetTxsResult", "blockHash", "offset", "num")]
        public async Task<JObject> GetTxsResult(string blockHash, int offset = 0, int num = 10)
        {
            if (offset < 0)
            {
                throw new JsonRpcServiceException(ChainRpcErrorConsts.InvalidOffset,
                    ChainRpcErrorConsts.RpcErrorMessage[ChainRpcErrorConsts.InvalidOffset]);
            }

            if (num <= 0 || num > 100)
            {
                throw new JsonRpcServiceException(ChainRpcErrorConsts.InvalidNum,
                    ChainRpcErrorConsts.RpcErrorMessage[ChainRpcErrorConsts.InvalidNum]);
            }

            Hash realBlockHash;
            try
            {
                realBlockHash = Hash.LoadHex(blockHash);
            }
            catch
            {
                throw new JsonRpcServiceException(ChainRpcErrorConsts.InvalidBlockHash,
                    ChainRpcErrorConsts.RpcErrorMessage[ChainRpcErrorConsts.InvalidBlockHash]);
            }

            var block = await this.GetBlock(_chainId, realBlockHash);
            if (block == null)
            {
                throw new JsonRpcServiceException(ChainRpcErrorConsts.NotFound,
                    ChainRpcErrorConsts.RpcErrorMessage[ChainRpcErrorConsts.NotFound]);
            }

            var txs = new JArray();

            if (offset <= block.Body.Transactions.Count - 1)
            {
                num = Math.Min(num, block.Body.Transactions.Count - offset);

                var txHashs = block.Body.Transactions.ToList().GetRange(offset, num);
                foreach (var hash in txHashs)
                {
                    txs.Add(await GetTx(hash));
                }
            }

            return new JObject {txs};
        }

        private async Task<JObject> GetTx(Hash txHash)
        {
            var receipt = await this.GetTransactionReceipt(txHash);
            if (receipt == null)
            {
                throw new JsonRpcServiceException(ChainRpcErrorConsts.NotFound,
                    ChainRpcErrorConsts.RpcErrorMessage[ChainRpcErrorConsts.NotFound]);
            }

            var transaction = receipt.Transaction;
            var txInfo = transaction.GetTransactionInfo();
            try
            {
                ((JObject) txInfo["Tx"]).Add("params",
                    (JObject) JsonConvert.DeserializeObject(await this.GetTransactionParameters(_chainId, transaction))
                );
            }
            catch (Exception)
            {
                // TODO: Why ignore?
                // Ignore for now
            }

            ((JObject) txInfo["Tx"]).Add("SignatureState", receipt.SignatureStatus.ToString());
            ((JObject) txInfo["Tx"]).Add("RefBlockState", receipt.RefBlockStatus.ToString());
            ((JObject) txInfo["Tx"]).Add("ExecutionState", receipt.TransactionStatus.ToString());
            ((JObject) txInfo["Tx"]).Add("ExecutedInBlock", receipt.ExecutedBlockNumber);

            var txResult = await this.GetTransactionResult(txHash);
            var response = new JObject
            {
                ["TxStatus"] = txResult.Status.ToString(),
                ["TxInfo"] = txInfo["Tx"]
            };
            var txtrc = await this.GetTransactionTrace(_chainId, txHash, txResult.BlockNumber);

#if DEBUG
            response["TxTrc"] = txtrc?.ToString();
#endif

            if (txResult.Status == TransactionResultStatus.Failed)
            {
                response["TxError"] = txResult.RetVal.ToStringUtf8();
            }

            if (txResult.Status == TransactionResultStatus.Mined)
            {
                response["Bloom"] = txResult.Bloom.ToByteArray().ToHex();
                response["Logs"] = (JArray) JsonConvert.DeserializeObject(txResult.Logs.ToString());
                response["BlockNumber"] = txResult.BlockNumber;
                response["BlockHash"] = txResult.BlockHash.ToHex();
                response["ReturnType"] = txtrc?.RetVal.Type.ToString();
                try
                {
                    if (txtrc?.RetVal.Type == RetVal.Types.RetType.String)
                    {
                        response["Return"] = txResult.RetVal.ToStringUtf8();
                    }
                    else
                        response["Return"] = Address.FromBytes(txResult.RetVal.ToByteArray()).GetFormatted();
                }
                catch (Exception)
                {
                    // not an error
                    response["Return"] = txResult.RetVal.ToByteArray().ToHex();
                }
            }
            // Todo: it should be deserialized to obj ion cli, 

            return response;
        }

        [JsonRpcMethod("GetBlockHeight")]
        public async Task<JObject> GetBlockHeight()
        {
            var height = await this.GetCurrentChainHeight(_chainId);
            var response = new JObject
            {
                ["BlockHeight"] = height.ToString()
            };
            return response;
        }

        [JsonRpcMethod("GetBlockInfo", "blockHeight", "includeTxs")]
        public async Task<JObject> GetBlockInfo(ulong blockHeight, bool includeTxs = false)
        {
            var blockInfo = await this.GetBlockAtHeight(_chainId, blockHeight);
            if (blockInfo == null)
            {
                throw new JsonRpcServiceException(ChainRpcErrorConsts.NotFound,
                    ChainRpcErrorConsts.RpcErrorMessage[ChainRpcErrorConsts.NotFound]);
            }

            // TODO: Create DTO Exntension for Block
            var response = new JObject
            {
                ["BlockHash"] = blockInfo.GetHash().ToHex(),
                ["Header"] = new JObject
                {
                    ["PreviousBlockHash"] = blockInfo.Header.PreviousBlockHash.ToHex(),
                    ["MerkleTreeRootOfTransactions"] = blockInfo.Header.MerkleTreeRootOfTransactions.ToHex(),
                    ["MerkleTreeRootOfWorldState"] = blockInfo.Header.MerkleTreeRootOfWorldState.ToHex(),
                    ["SideChainTransactionsRoot"] = blockInfo.Header.SideChainTransactionsRoot?.ToHex(),
                    ["Height"] = blockInfo.Header.Height.ToString(),
                    ["Time"] = blockInfo.Header.Time.ToDateTime(),
                    ["ChainId"] = blockInfo.Header.ChainId.DumpBase58(),
                    ["Bloom"] = blockInfo.Header.Bloom.ToByteArray().ToHex()
                    //["IndexedInfo"] = blockinfo.Header.GetIndexedSideChainBlcokInfo()
                },
                ["Body"] = new JObject
                {
                    ["TransactionsCount"] = blockInfo.Body.TransactionsCount,
                    ["IndexedSideChainBlockInfo"] = await this.GetIndexedSideChainBlockInfo(_chainId, blockInfo.Header.Height)
                }
            };

            if (includeTxs)
            {
                var transactions = blockInfo.Body.Transactions;
                var txs = new List<string>();
                foreach (var txHash in transactions)
                {
                    txs.Add(txHash.ToHex());
                }

                response["Body"]["Transactions"] = JArray.FromObject(txs);
            }

            return response;
        }

        [JsonRpcMethod("GetTxPoolSize")]
        public async Task<JObject> GetTxPoolSize()
        {
            var transactionPoolSize = await this.GetTransactionPoolSize();
            var response = new JObject
            {
                ["CurrentTransactionPoolSize"] = transactionPoolSize
            };

            return response;
        }

        [JsonRpcMethod("GetDposStatus")]
        public async Task<JObject> GetDposStatus()
        {
            var isAlive = await MainchainNodeService.CheckDPoSAliveAsync();
            var response = new JObject
            {
                ["IsAlive"] = isAlive
            };

            return response;
        }

        [JsonRpcMethod("GetNodeStatus")]
        public async Task<JObject> GetNodeStatus()
        {
            var isForked = await MainchainNodeService.CheckForkedAsync();
            var response = new JObject
            {
                ["IsForked"] = isForked
            };

            return response;
        }

        #endregion Methods

        #region Proposal

        [JsonRpcMethod("GetProposal", "proposalId")]
        public async Task<JObject> GetProposal(string proposalId)
        {
            Hash proposalHash;
            try
            {
                proposalHash = Hash.LoadHex(proposalId);
            }
            catch
            {
                throw new JsonRpcServiceException(ChainRpcErrorConsts.InvalidProposalId,
                    ChainRpcErrorConsts.RpcErrorMessage[ChainRpcErrorConsts.InvalidProposalId]);
            }

            var proposal = await this.GetProposal(_chainId, proposalHash);
            if (proposal == null)
            {
                throw new JsonRpcServiceException(ChainRpcErrorConsts.NotFound,
                    ChainRpcErrorConsts.RpcErrorMessage[ChainRpcErrorConsts.NotFound]);
            }

            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return new JObject
            {
                ["ProposalName"] = proposal.Name,
                ["MultiSig"] = proposal.MultiSigAccount.GetFormatted(),
                ["ExpiredTime"] = origin.AddSeconds(proposal.ExpiredTime),
                ["TxnData"] = proposal.TxnData.ToByteArray().ToHex(),
                ["Status"] = proposal.Status.ToString(),
                ["Proposer"] = proposal.Proposer.GetFormatted()
            };
        }

        #endregion

        #region Admin

        [JsonRpcMethod("GetInvalidBlockCount")]
        public async Task<JObject> GetInvalidBlockCount()
        {
            var invalidBlockCount = await this.GetInvalidBlockCountAsync();

            var response = new JObject
            {
                ["InvalidBlockCount"] = invalidBlockCount
            };

            return response;
        }

        [JsonRpcMethod("GetRollBackTimes")]
        public async Task<JObject> GetRollBackTimes()
        {
            var rollBackTimes = await this.GetRollBackTimesAsync();

            var response = new JObject
            {
                ["RollBackTimes"] = rollBackTimes
            };

            return response;
        }

        [JsonRpcMethod("GetBlockStateSet", "blockHash")]
        public async Task<JObject> GetBlockStateSet(string blockHash)
        {
            var obj = await BlockStateSets.GetAsync(blockHash);
            return JObject.FromObject(JsonConvert.DeserializeObject(obj.ToString()));
        }

        #endregion Methods
    }
}
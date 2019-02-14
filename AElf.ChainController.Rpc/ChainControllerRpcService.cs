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
            try
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
            catch (Exception e)
            {
                return await Task.FromResult(new JObject
                {
                    ["Error"] = e.ToString()
                });
            }
        }

        [JsonRpcMethod("GetChainInfo")]
        public async Task<JObject> GetChainInfo()
        {
            try
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

                return await Task.FromResult(JObject.FromObject(response));
            }
            catch (Exception e)
            {
                var response = new JObject
                {
                    ["Exception"] = e.ToString()
                };

                return await Task.FromResult(JObject.FromObject(response));
            }
        }

        [JsonRpcMethod("GetContractAbi", "address")]
        public async Task<JObject> GetContractAbi(string address)
        {
            try
            {
                var addrHash = Address.Parse(address);

                var abi = await this.GetContractAbi(_chainId, addrHash);

                return new JObject
                {
                    ["Address"] = address,
                    ["Abi"] = abi.ToByteArray().ToHex(),
                    ["Error"] = ""
                };
            }
            catch (Exception)
            {
                return new JObject
                {
                    ["Address"] = address,
                    ["Abi"] = "",
                    ["Error"] = "Not Found"
                };
            }
        }

        [JsonRpcMethod("Call", "rawTx")]
        public async Task<JObject> CallReadOnly(string rawTx)
        {
            var hexString = ByteArrayHelpers.FromHexString(rawTx);
            var transaction = Transaction.Parser.ParseFrom(hexString);

            JObject response;
            try
            {
                var res = await this.CallReadOnly(_chainId, transaction);
                response = new JObject
                {
                    ["Return"] = res?.ToHex()
                };
            }
            catch (Exception e)
            {
                response = new JObject
                {
                    ["Error"] = e.ToString()
                };
            }

            return JObject.FromObject(response);
        }

        [JsonRpcMethod("BroadcastTx", "rawTx")]
        public async Task<JObject> BroadcastTx(string rawTx)
        {
            var hexString = ByteArrayHelpers.FromHexString(rawTx);
            var transaction = Transaction.Parser.ParseFrom(hexString);

            var res = new JObject {["Hash"] = transaction.GetHash().ToHex()};

            if (!_canBroadcastTxs)
            {
                res["Error"] = "Sync still in progress, cannot send transactions.";
                return res;
            }

            try
            {
                //TODO: Wait validation done
                transaction.GetTransactionInfo();
                await TxHub.AddTransactionAsync(_chainId, transaction);
            }
            catch (Exception e)
            {
                res["Error"] = e.ToString();
            }

            return res;
        }

        [JsonRpcMethod("BroadcastTxs", "rawTxs")]
        public async Task<JObject> BroadcastTxs(string rawTxs)
        {
            var response = new List<object>();

            if (!_canBroadcastTxs)
            {
                return new JObject
                {
                    ["Result"] = JToken.FromObject(string.Empty),
                    ["Error"] = "Sync still in progress, cannot send transactions."
                };
            }

            foreach (var rawTx in rawTxs.Split(','))
            {
                var result = await BroadcastTx(rawTx);
                if (result.ContainsKey("Error"))
                    break;
                response.Add(result["Hash"].ToString());
            }

            return new JObject
            {
                ["Result"] = JToken.FromObject(response)
            };
        }

        [JsonRpcMethod("GetTxMerklePath", "txId")]
        public async Task<JObject> GetTxMerklePath(string txId)
        {
            try
            {
                Hash txHash;
                try
                {
                    txHash = Hash.LoadHex(txId);
                }
                catch (Exception)
                {
                    throw new Exception("Invalid Address Format");
                }

                var txResult = await this.GetTransactionResult(txHash);
                /*if(txResult.Status != Status.Mined)
                   throw new Exception("Transaction is not mined.");*/
                var binaryMerkleTree = await this.GetBinaryMerkleTreeByHeight(_chainId, txResult.BlockNumber);
                var merklePath = binaryMerkleTree.GenerateMerklePath(txResult.Index);
                if (merklePath == null)
                    throw new Exception("Not found merkle path for this transaction.");
                MerklePath merklePathInParentChain = null;
                ulong boundParentChainHeight = 0;
                try
                {
                    merklePathInParentChain = await this.GetTxRootMerklePathInParentChain(_chainId, txResult.BlockNumber);
                    boundParentChainHeight = await this.GetBoundParentChainHeight(_chainId, txResult.BlockNumber);
                }
                catch (Exception e)
                {
                    throw new Exception($"Unable to get merkle path from parent chain {e}");
                }

                /*if(merklePathInParentChain == null)
                    throw new Exception("Not found merkle path in parent chain");*/
                if (merklePathInParentChain != null)
                    merklePath.Path.AddRange(merklePathInParentChain.Path);
                return new JObject
                {
                    ["MerklePath"] = merklePath.ToByteArray().ToHex(),
                    ["ParentHeight"] = boundParentChainHeight
                };
            }
            catch (Exception e)
            {
                return new JObject
                {
                    ["Error"] = e.Message
                };
            }
        }

        [JsonRpcMethod("GetParentChainBlockInfo", "height")]
        public async Task<JObject> GetParentChainBlockInfo(string height)
        {
            try
            {
                ulong h;
                try
                {
                    h = ulong.Parse(height);
                }
                catch (Exception)
                {
                    throw new Exception("Invalid height");
                }
                var merklePathInParentChain = await this.GetParentChainBlockInfo(_chainId, h);
                if (merklePathInParentChain == null)
                {
                    throw new Exception("Unable to get parent chain block at height " + height);
                }

                return new JObject
                {
                    ["ParentChainId"] = merklePathInParentChain.Root.ChainId.DumpBase58(),
                    ["SideChainTxsRoot"] = merklePathInParentChain.Root.SideChainTransactionsRoot.ToHex(),
                    ["ParentHeight"] = merklePathInParentChain.Height
                };
            }
            catch (Exception e)
            {
                return new JObject
                {
                    ["Error"] = e.Message
                };
            }
        }

        [JsonRpcMethod("GetTxResult", "txHash")]
        public async Task<JObject> GetTxResult(string txHash)
        {
            Hash txId;
            try
            {
                txId = Hash.LoadHex(txHash);
            }
            catch
            {
                return JObject.FromObject(new JObject
                {
                    ["Error"] = "Invalid Format"
                });
            }

            try
            {
                var response = await GetTx(txId);
                return JObject.FromObject(new JObject {response});
            }
            catch (Exception e)
            {
                return new JObject
                {
                    ["Error"] = e.Message
                };
            }
        }

        [JsonRpcMethod("GetTxsResult", "blockHash", "offset", "num")]
        public async Task<JObject> GetTxsResult(string blockHash, int offset = 0, int num = 10)
        {
            if (offset < 0)
            {
                return JObject.FromObject(new JObject
                {
                    ["Error"] = "offset must greater than or equal to 0."
                });
            }

            if (num <= 0 || num > 100)
            {
                return JObject.FromObject(new JObject
                {
                    ["Error"] = "num must between 0 and 100."
                });
            }

            Hash realBlockHash;
            try
            {
                realBlockHash = Hash.LoadHex(blockHash);
            }
            catch
            {
                return JObject.FromObject(new JObject
                {
                    ["Error"] = "Invalid Block Hash Format"
                });
            }

            try
            {
                var block = await this.GetBlock(_chainId, realBlockHash);
                if (block == null)
                {
                    return JObject.FromObject(new JObject
                    {
                        ["Error"] = "Invalid Block Hash"
                    });
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

                return JObject.FromObject(new JObject {txs});
            }
            catch (Exception e)
            {
                return new JObject
                {
                    ["Error"] = e.Message
                };
            }
        }

        private async Task<JObject> GetTx(Hash txHash)
        {
            var receipt = await this.GetTransactionReceipt(txHash);
            JObject txInfo = null;
            if (receipt != null)
            {
                var transaction = receipt.Transaction;
                txInfo = transaction.GetTransactionInfo();
                try
                {
                    ((JObject) txInfo["Tx"]).Add("params",
                        (JObject) JsonConvert.DeserializeObject(await this.GetTransactionParameters(_chainId, transaction))
                    );
                }
                catch (Exception)
                {
                    // Ignore for now
                }

                ((JObject) txInfo["Tx"]).Add("SignatureState", receipt.SignatureStatus.ToString());
                ((JObject) txInfo["Tx"]).Add("RefBlockState", receipt.RefBlockStatus.ToString());
                ((JObject) txInfo["Tx"]).Add("ExecutionState", receipt.TransactionStatus.ToString());
                ((JObject) txInfo["Tx"]).Add("ExecutedInBlock", receipt.ExecutedBlockNumber);
            }
            else
            {
                txInfo = new JObject {["Tx"] = "Not Found"};
            }

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
            return JObject.FromObject(response);
        }

        [JsonRpcMethod("GetBlockInfo", "blockHeight", "includeTxs")]
        public async Task<JObject> GetBlockInfo(string blockHeight, bool includeTxs = false)
        {
            var invalidBlockHeightError = JObject.FromObject(new JObject
            {
                ["error"] = "Invalid Block Height"
            });

            if (!ulong.TryParse(blockHeight, out var height))
            {
                return invalidBlockHeightError;
            }

            var blockinfo = await this.GetBlockAtHeight(_chainId, height);
            if (blockinfo == null)
                return invalidBlockHeightError;

            // TODO: Create DTO Exntension for Block
            var response = new JObject
            {
                ["BlockHash"] = blockinfo.GetHash().ToHex(),
                ["Header"] = new JObject
                {
                    ["PreviousBlockHash"] = blockinfo.Header.PreviousBlockHash.ToHex(),
                    ["MerkleTreeRootOfTransactions"] = blockinfo.Header.MerkleTreeRootOfTransactions.ToHex(),
                    ["MerkleTreeRootOfWorldState"] = blockinfo.Header.MerkleTreeRootOfWorldState.ToHex(),
                    ["SideChainTransactionsRoot"] = blockinfo.Header.SideChainTransactionsRoot?.ToHex(),
                    ["Height"] = blockinfo.Header.Height.ToString(),
                    ["Time"] = blockinfo.Header.Time.ToDateTime(),
                    ["ChainId"] = blockinfo.Header.ChainId.DumpBase58(),
                    ["Bloom"] = blockinfo.Header.Bloom.ToByteArray().ToHex()
                    //["IndexedInfo"] = blockinfo.Header.GetIndexedSideChainBlcokInfo()
                },
                ["Body"] = new JObject
                {
                    ["TransactionsCount"] = blockinfo.Body.TransactionsCount,
                    ["IndexedSideChainBlockInfo"] = await this.GetIndexedSideChainBlockInfo(_chainId, blockinfo.Header.Height)
                }
            };

            if (includeTxs)
            {
                var transactions = blockinfo.Body.Transactions;
                var txs = new List<string>();
                foreach (var txHash in transactions)
                {
                    txs.Add(txHash.ToHex());
                }

                response["Body"]["Transactions"] = JArray.FromObject(txs);
            }

            return JObject.FromObject(response);
        }

        [JsonRpcMethod("GetTxPoolSize")]
        public async Task<JObject> GetTxPoolSize()
        {
            var transactionPoolSize = await this.GetTransactionPoolSize();
            var response = new JObject
            {
                ["CurrentTransactionPoolSize"] = transactionPoolSize
            };

            return JObject.FromObject(response);
        }

        [JsonRpcMethod("GetDposStatus")]
        public async Task<JObject> GetDposStatus()
        {
            var isAlive = await MainchainNodeService.CheckDPoSAliveAsync();
            var response = new JObject
            {
                ["IsAlive"] = isAlive
            };

            return JObject.FromObject(response);
        }

        [JsonRpcMethod("GetNodeStatus")]
        public async Task<JObject> GetNodeStatus()
        {
            var isForked = await MainchainNodeService.CheckForkedAsync();
            var response = new JObject
            {
                ["IsForked"] = isForked
            };

            return JObject.FromObject(response);
        }

        #endregion Methods

        #region Proposal

        [JsonRpcMethod("GetProposal", "proposalId")]
        public async Task<JObject> GetProposal(string proposalId)
        {
            try
            {
                Hash proposalHash;
                try
                {
                    proposalHash = Hash.LoadHex(proposalId);
                }
                catch (Exception)
                {
                    throw new Exception("Invalid Hash Format");
                }

                var proposal = await this.GetProposal(_chainId, proposalHash);
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
            catch (Exception e)
            {
                return new JObject
                {
                    ["Error"] = e.Message
                };
            }
        }

        #endregion

        #region Consensus

        public async Task<JObject> VotesGeneral()
        {
            try
            {
                var general = await this.GetVotesGeneral(_chainId);
                return new JObject
                {
                    ["Error"] = 0,
                    ["VotersCount"] = general.Item1,
                    ["TicketsCount"] = general.Item2,
                };
            }
            catch (Exception e)
            {
                return new JObject
                {
                    ["Error"] = 1,
                    ["ErrorMsg"] = e.Message
                };
            }
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

            return JObject.FromObject(response);
        }

        [JsonRpcMethod("GetRollBackTimes")]
        public async Task<JObject> GetRollBackTimes()
        {
            var rollBackTimes = await this.GetRollBackTimesAsync();

            var response = new JObject
            {
                ["RollBackTimes"] = rollBackTimes
            };

            return JObject.FromObject(response);
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
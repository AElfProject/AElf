using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Domain;
using AElf.Kernel.Node.Domain;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContractExecution.Domain;
using AElf.Kernel.TransactionPool.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using Anemonis.AspNetCore.JsonRpc;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Volo.Abp.EventBus.Local;

namespace AElf.OS.Rpc.ChainController
{
    [Path("/chain")]
    public class ChainControllerRpcService : IJsonRpcService
    {
        public IBlockchainService BlockchainService { get; set; }
        public IChainRelatedComponentManager<ITxHub> TxHubs { get; set; }
        public ITransactionResultManager TransactionResultManager { get; set; }
        public ITransactionTraceManager TransactionTraceManager { get; set; }
        public ISmartContractExecutiveService SmartContractExecutiveService { get; set; }

        // public INodeService MainchainNodeService { get; set; }
        // public ICrossChainInfoReader CrossChainInfoReader { get; set; }
        // public IAuthorizationInfoReader AuthorizationInfoReader { get; set; }
        // public IBlockSynchronizer BlockSynchronizer { get; set; }

        public IBinaryMerkleTreeManager BinaryMerkleTreeManager { get; set; }
        public IStateStore<BlockStateSet> BlockStateSets { get; set; }
        public ILogger<ChainControllerRpcService> Logger { get; set; }

        private readonly ChainOptions _chainOptions;

        public int ChainId => _chainOptions.ChainId;
        public ILocalEventBus LocalEventBus { get; set; } = NullLocalEventBus.Instance;
        public ITxHub TxHub => TxHubs.Get(_chainOptions.ChainId);

        public ChainControllerRpcService(IOptionsSnapshot<ChainOptions> options)
        {
            Logger = NullLogger<ChainControllerRpcService>.Instance;
            _chainOptions = options.Value;
        }

        [JsonRpcMethod("GetCommands")]
        public async Task<JArray> GetCommands()
        {
            var methodContracts = this.GetRpcMethodContracts();
            var commands = methodContracts.Keys.OrderBy(x => x).ToList();
            var json = JsonConvert.SerializeObject(commands);
            var commandArray = JArray.Parse(json);

            return await Task.FromResult(commandArray);
        }

        [JsonRpcMethod("ConnectChain")]
        public Task<JObject> GetChainInfo()
        {
            var basicContractZero = Address.BuildContractAddress(_chainOptions.ChainId, 0);

            var response = new JObject
            {
                [SmartContract.GenesisSmartContractZeroAssemblyName] = basicContractZero.GetFormatted(),
                ["ChainId"] = ChainHelpers.ConvertChainIdToBase58(_chainOptions.ChainId)
            };

            return Task.FromResult(response);
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
                throw new JsonRpcServiceException(Error.InvalidAddress, Error.Message[Error.InvalidAddress]);
            }

            var abi = await this.GetContractAbi(_chainOptions.ChainId, addressHash);

            if (abi == null)
            {
                throw new JsonRpcServiceException(Error.NotFound, Error.Message[Error.NotFound]);
            }

            return new JObject
            {
                ["Address"] = address,
                ["Abi"] = abi.ToByteArray().ToHex()
            };
        }

        [JsonRpcMethod("Call", "rawTransaction")]
        public async Task<string> CallReadOnly(string rawTransaction)
        {
            byte[] response;
            try
            {
                var hexString = ByteArrayHelpers.FromHexString(rawTransaction);
                var transaction = Transaction.Parser.ParseFrom(hexString);
                response = await this.CallReadOnly(_chainOptions.ChainId, transaction);
            }
            catch
            {
                throw new JsonRpcServiceException(Error.InvalidTransaction, Error.Message[Error.InvalidTransaction]);
            }

            return response?.ToHex();
        }

        [JsonRpcMethod("BroadcastTransaction", "rawTransaction")]
        public async Task<JObject> BroadcastTransaction(string rawTransaction)
        {
            var txIds = await this.PublishTransactionsAsync(new string[]{rawTransaction});
            var response = new JObject {["TransactionId"] = txIds[0]};
            return response;
        }
        
        [JsonRpcMethod("BroadcastTransactions", "rawTransactions")]
        public async Task<JArray> BroadcastTransactions(string rawTransactions)
        {
            var txIds = await this.PublishTransactionsAsync(rawTransactions.Split(","));

            return JArray.FromObject(txIds);
        }

        [JsonRpcMethod("GetTransactionResult", "transactionId")]
        public async Task<JObject> GetTransactionResult(string transactionId)
        {
            Hash transactionHash;
            try
            {
                transactionHash = Hash.LoadHex(transactionId);
            }
            catch
            {
                throw new JsonRpcServiceException(Error.InvalidTransactionId,
                    Error.Message[Error.InvalidTransactionId]);
            }

            var response = await GetTransaction(transactionHash);
            return response;
        }

        [JsonRpcMethod("GetTransactionsResult", "blockHash", "offset", "num")]
        public async Task<JObject> GetTransactionsResult(string blockHash, int offset = 0, int num = 10)
        {
            if (offset < 0)
            {
                throw new JsonRpcServiceException(Error.InvalidOffset,
                    Error.Message[Error.InvalidOffset]);
            }

            if (num <= 0 || num > 100)
            {
                throw new JsonRpcServiceException(Error.InvalidNum, Error.Message[Error.InvalidNum]);
            }

            Hash realBlockHash;
            try
            {
                realBlockHash = Hash.LoadHex(blockHash);
            }
            catch
            {
                throw new JsonRpcServiceException(Error.InvalidBlockHash, Error.Message[Error.InvalidBlockHash]);
            }

            var block = await this.GetBlock(_chainOptions.ChainId, realBlockHash);
            if (block == null)
            {
                throw new JsonRpcServiceException(Error.NotFound, Error.Message[Error.NotFound]);
            }

            var transactions = new JArray();

            if (offset <= block.Body.Transactions.Count - 1)
            {
                num = Math.Min(num, block.Body.Transactions.Count - offset);

                var transactionHashs = block.Body.Transactions.ToList().GetRange(offset, num);
                foreach (var hash in transactionHashs)
                {
                    transactions.Add(await GetTransaction(hash));
                }
            }

            return new JObject {transactions};
        }

        private async Task<JObject> GetTransaction(Hash transactionHash)
        {
            var transactionResult = await this.GetTransactionResult(transactionHash);
            return (JObject) JsonConvert.DeserializeObject(transactionResult.ToString());
//            var receipt = await this.GetTransactionReceipt(transactionHash);
//            if (receipt == null)
//            {
//                throw new JsonRpcServiceException(Error.NotFound, Error.Message[Error.NotFound]);
//            }
//
//            var transaction = receipt.Transaction;
//            var transactionInfo = transaction.GetTransactionInfo();
//            try
//            {
//                ((JObject) transactionInfo["Transaction"]).Add("params",
//                    (JObject) JsonConvert.DeserializeObject(
//                        await this.GetTransactionParameters(_chainOptions.ChainId, transaction))
//                );
//            }
//            catch (Exception)
//            {
//                // TODO: Why ignore?
//                // Ignore for now
//            }
//
//            ((JObject) transactionInfo["Transaction"]).Add("SignatureState", receipt.SignatureStatus.ToString());
//            ((JObject) transactionInfo["Transaction"]).Add("RefBlockState", receipt.RefBlockStatus.ToString());
//            ((JObject) transactionInfo["Transaction"]).Add("ExecutionState", receipt.TransactionStatus.ToString());
//            ((JObject) transactionInfo["Transaction"]).Add("ExecutedInBlock", receipt.ExecutedBlockNumber);
//
//            var transactionResult = await this.GetTransactionResult(transactionHash);
//            var response = new JObject
//            {
//                ["TransactionStatus"] = transactionResult.Status.ToString(),
//                ["TransactionInfo"] = transactionInfo["Transaction"]
//            };
//            var transactionTrace =
//                await this.GetTransactionTrace(_chainOptions.ChainId, transactionHash, transactionResult.BlockNumber);
//
//#if DEBUG
//            response["TransactionTrace"] = transactionTrace?.ToString();
//#endif
//
//            if (transactionResult.Status == TransactionResultStatus.Failed)
//            {
//                response["TransactionError"] = transactionResult.RetVal.ToStringUtf8();
//            }
//
//            if (transactionResult.Status == TransactionResultStatus.Mined)
//            {
//                response["Bloom"] = transactionResult.Bloom.ToByteArray().ToHex();
//                response["Logs"] = (JArray) JsonConvert.DeserializeObject(transactionResult.Logs.ToString());
//                response["BlockNumber"] = transactionResult.BlockNumber;
//                response["BlockHash"] = transactionResult.BlockHash.ToHex();
//                response["ReturnType"] = transactionTrace?.RetVal.Type.ToString();
//                try
//                {
//                    if (transactionTrace?.RetVal.Type == RetVal.Types.RetType.String)
//                    {
//                        response["ReturnValue"] = transactionResult.RetVal.ToStringUtf8();
//                    }
//                    else
//                        response["ReturnValue"] =
//                            Address.FromBytes(transactionResult.RetVal.ToByteArray()).GetFormatted();
//                }
//                catch (Exception)
//                {
//                    // not an error`
//                    response["ReturnValue"] = transactionResult.RetVal.ToByteArray().ToHex();
//                }
//            }
            // Todo: it should be deserialized to obj ion cli, 

//            return response;
        }

        [JsonRpcMethod("GetBlockHeight")]
        public async Task<ulong> GetBlockHeight()
        {
            return await this.GetCurrentChainHeight(_chainOptions.ChainId);
        }

        [JsonRpcMethod("GetBlockInfo", "blockHeight", "includeTransactions")]
        public async Task<JObject> GetBlockInfo(ulong blockHeight, bool includeTransactions = false)
        {
            var blockInfo = await this.GetBlockAtHeight(_chainOptions.ChainId, blockHeight);
            if (blockInfo == null)
            {
                throw new JsonRpcServiceException(Error.NotFound, Error.Message[Error.NotFound]);
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
                    ["SideChainTransactionsRoot"] = blockInfo.Header.BlockExtraData.SideChainTransactionsRoot?.ToHex(),
                    ["Height"] = blockInfo.Header.Height.ToString(),
                    ["Time"] = blockInfo.Header.Time.ToDateTime(),
                    ["ChainId"] = ChainHelpers.ConvertChainIdToBase58(blockInfo.Header.ChainId),
                    ["Bloom"] = blockInfo.Header.Bloom.ToByteArray().ToHex()
                },
                ["Body"] = new JObject
                {
                    ["TransactionsCount"] = blockInfo.Body.TransactionsCount,
                    // ["IndexedSideChainBlockInfo"] = await this.GetIndexedSideChainBlockInfo(_chainId, blockInfo.Header.Height)
                }
            };

            if (includeTransactions)
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
//
//        [JsonRpcMethod("GetTransactionPoolSize")]
//        public async Task<ulong> GetTxPoolSize()
//        {
//            return await this.GetTransactionPoolSize();
//        }

        [JsonRpcMethod("GetBlockStateSet", "blockHash")]
        public async Task<JObject> GetBlockStateSet(string blockHash)
        {
            var obj = await BlockStateSets.GetAsync(blockHash);
            return JObject.FromObject(JsonConvert.DeserializeObject(obj.ToString()));
        }
        
        [JsonRpcMethod("GetChainStatus")]	
        public async Task<JObject> GetChainStatus()	
        {	
            var chain = await BlockchainService.GetChainAsync(_chainOptions.ChainId);	
            var branches = (JObject) JsonConvert.DeserializeObject(chain.Branches.ToString());	
            var notLinkedBlocks = (JObject) JsonConvert.DeserializeObject(chain.NotLinkedBlocks.ToString());	
            return new JObject	
            {	
                ["Branches"] = branches,	
                ["NotLinkedBlocks"] = notLinkedBlocks,	
                ["LongestChainHeight"] = chain.LongestChainHeight,	
                ["LongestChainHash"] = chain.LongestChainHash?.ToHex(),	
                ["GenesisBlockHash"] = chain.GenesisBlockHash.ToHex(),	
                ["LastIrreversibleBlockHash"] = chain.LastIrreversibleBlockHash?.ToHex(),	
                ["LastIrreversibleBlockHeight"] = chain.LastIrreversibleBlockHeight,	
                ["BestChainHash"] = chain.BestChainHash?.ToHex(),	
                ["BestChainHeight"] = chain.BestChainHeight	
            };	
        }

        /*
        [JsonRpcMethod("GetConsensusStatus")]
        public async Task<JObject> GetConsensusStatus()
        {
            var isAlive = await MainchainNodeService.CheckDPoSAliveAsync();
            var response = new JObject
            {
                ["IsAlive"] = isAlive
            };

            return response;
        }
        */

        /*
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
                throw new JsonRpcServiceException(Error.InvalidProposalId, Error.Message[Error.InvalidProposalId]);
            }

            var proposal = await this.GetProposal(_chainId, proposalHash);
            if (proposal == null)
            {
                throw new JsonRpcServiceException(Error.NotFound, Error.Message[Error.NotFound]);
            }

            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return new JObject
            {
                ["ProposalName"] = proposal.Name,
                ["MultiSig"] = proposal.MultiSigAccount.GetFormatted(),
                ["ExpiredTime"] = origin.AddSeconds(proposal.ExpiredTime),
                ["Transaction"] = proposal.TxnData.ToByteArray().ToHex(),
                ["Status"] = proposal.Status.ToString(),
                ["Proposer"] = proposal.Proposer.GetFormatted()
            };
        }
        */

        /*
        [JsonRpcMethod("GetTransactionMerklePath", "transactionId")]
        public async Task<JObject> GetTransactionMerklePath(string transactionId)
        {
            Hash transactionHash;
            try
            {
                transactionHash = Hash.LoadHex(transactionId);
            }
            catch
            {
                throw new JsonRpcServiceException(Error.InvalidTransactionId, Error.Message[Error.InvalidTransactionId]);
            }

            var transactionResult = await this.GetTransactionResult(transactionHash);
            if (transactionResult == null)
            {
                throw new JsonRpcServiceException(Error.NotFound, Error.Message[Error.NotFound]);
            }

            var binaryMerkleTree = await this.GetBinaryMerkleTreeByHeight(_chainId, transactionResult.BlockNumber);
            var merklePath = binaryMerkleTree.GenerateMerklePath(transactionResult.Index);
            if (merklePath == null)
            {
                throw new JsonRpcServiceException(Error.NotFound, Error.Message[Error.NotFound]);
            }

            MerklePath merklePathInParentChain = null;
            ulong boundParentChainHeight = 0;
            try
            {
                merklePathInParentChain = await this.GetTxRootMerklePathInParentChain(_chainId, transactionResult.BlockNumber);
                boundParentChainHeight = await this.GetBoundParentChainHeight(_chainId, transactionResult.BlockNumber);
            }
            catch (Exception e)
            {
                throw new JsonRpcServiceException(Error.NotFound, Error.Message[Error.NotFound]);
            }

            if (merklePathInParentChain != null)
                merklePath.Path.AddRange(merklePathInParentChain.Path);
            return new JObject
            {
                ["MerklePath"] = merklePath.ToByteArray().ToHex(),
                ["ParentHeight"] = boundParentChainHeight
            };
        }
        */

        /*
        [JsonRpcMethod("GetParentChainBlockInfo", "height")]
        public async Task<JObject> GetParentChainBlockInfo(ulong height)
        {
            var merklePathInParentChain = await this.GetParentChainBlockInfo(_chainId, height);
            if (merklePathInParentChain == null)
            {
                throw new JsonRpcServiceException(Error.NotFound, Error.Message[Error.NotFound]);
            }

            return new JObject
            {
                ["ParentChainId"] = merklePathInParentChain.Root.ChainId.DumpBase58(),
                ["SideChainTransactionsRoot"] = merklePathInParentChain.Root.SideChainTransactionsRoot.ToHex(),
                ["ParentHeight"] = merklePathInParentChain.Height
            };
        }
        */
    }
}
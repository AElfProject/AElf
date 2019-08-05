using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Types;
using Anemonis.AspNetCore.JsonRpc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Volo.Abp.EventBus.Local;

namespace AElf.OS.Rpc.ChainController
{
    [Path("/chain")]
    public class ChainControllerRpcService : IJsonRpcService
    {
        public IBlockchainService BlockchainService { get; set; }
        public ITxHub TxHub { get; set; }
        public ITransactionReadOnlyExecutionService TransactionReadOnlyExecutionService { get; set; }
        public ITransactionResultQueryService TransactionResultQueryService { get; set; }
        public ITransactionManager TransactionManager { get; set; }
        public ISmartContractAddressService SmartContractAddressService { get; set; }
        //TODO: should not directly use BlockStateSets
        public IStateStore<BlockStateSet> BlockStateSets { get; set; }
        public ILogger<ChainControllerRpcService> Logger { get; set; }

        public ILocalEventBus LocalEventBus { get; set; } = NullLocalEventBus.Instance;

        public ChainControllerRpcService()
        {
            Logger = NullLogger<ChainControllerRpcService>.Instance;
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

        [JsonRpcMethod("GetChainInformation")]
        public Task<JObject> GetChainInformation()
        {
            //var map = SmartContractAddressService.GetSystemContractNameToAddressMapping();
            var basicContractZero = SmartContractAddressService.GetZeroSmartContractAddress();
            var response = new JObject
            {
                ["GenesisContractAddress"] = basicContractZero?.GetFormatted(),
                ["ChainId"] = ChainHelper.ConvertChainIdToBase58(BlockchainService.GetChainId())
            };

            return Task.FromResult(response);
        }

        [JsonRpcMethod("Call", "rawTransaction")]
        public async Task<string> CallReadOnly(string rawTransaction)
        {
            byte[] response;
            try
            {
                var hexString = ByteArrayHelper.HexStringToByteArray(rawTransaction);
                var transaction = Transaction.Parser.ParseFrom(hexString);
                response = await this.CallReadOnly(transaction);
            }
            catch
            {
                throw new JsonRpcServiceException(Error.InvalidTransaction, Error.Message[Error.InvalidTransaction]);
            }

            return response?.ToHex();
        }

        [JsonRpcMethod("GetFileDescriptorSet", "address")]
        public async Task<byte[]> GetFileDescriptorSet(string address)
        {
            try
            {
                return await this.GetFileDescriptorSetAsync(AddressHelper.Base58StringToAddress(address));
            }
            catch(Exception)
            {
                throw new JsonRpcServiceException(Error.NotFound, Error.Message[Error.NotFound]);
            }
        }

        [JsonRpcMethod("BroadcastTransaction", "rawTransaction")]
        public async Task<JObject> BroadcastTransaction(string rawTransaction)
        {
            var txIds = await this.PublishTransactionsAsync(new string[] {rawTransaction});
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
            Hash transactionIdHash;
            try
            {
                transactionIdHash = HashHelper.HexStringToHash(transactionId);
            }
            catch
            {
                throw new JsonRpcServiceException(Error.InvalidTransactionId, Error.Message[Error.InvalidTransactionId]);
            }

            var transactionResult = await this.GetTransactionResult(transactionIdHash);
            if (transactionResult.Status == TransactionResultStatus.NotExisted)
                return new JObject
                {
                    ["TransactionId"] = transactionId,
                    ["Status"] = nameof(TransactionResultStatus.NotExisted)
                };
            
            var transaction = await TransactionManager.GetTransaction(transactionResult.TransactionId);
            
            var response = (JObject) JsonConvert.DeserializeObject(transactionResult.ToString());
            response["TransactionId"] = transactionResult.TransactionId.ToHex();
            response["Status"] = transactionResult.Status.ToString();
            
            if (transactionResult.Status == TransactionResultStatus.Mined)
            {
                var block = await this.GetBlockAtHeight(transactionResult.BlockNumber);
                response["BlockHash"] = block.GetHash().ToHex();
            }

            if (transactionResult.Status == TransactionResultStatus.Failed)
                response["Error"] = transactionResult.Error;

            response["Transaction"] = (JObject) JsonConvert.DeserializeObject(transaction.ToString());

            return response;
        }

        [JsonRpcMethod("GetTransactionsResult", "blockHash", "offset", "limit")]
        public async Task<JArray> GetTransactionsResult(string blockHash, int offset = 0, int limit = 10)
        {
            if (offset < 0)
            {
                throw new JsonRpcServiceException(Error.InvalidOffset, Error.Message[Error.InvalidOffset]);
            }

            if (limit <= 0 || limit > 100)
            {
                throw new JsonRpcServiceException(Error.InvalidNum, Error.Message[Error.InvalidNum]);
            }

            Hash realBlockHash;
            try
            {
                realBlockHash = HashHelper.HexStringToHash(blockHash);
            }
            catch
            {
                throw new JsonRpcServiceException(Error.InvalidBlockHash, Error.Message[Error.InvalidBlockHash]);
            }

            var block = await this.GetBlock(realBlockHash);
            if (block == null)
            {
                throw new JsonRpcServiceException(Error.NotFound, Error.Message[Error.NotFound]);
            }

            var response = new JArray();
            if (offset <= block.Body.TransactionIds.Count - 1)
            {
                limit = Math.Min(limit, block.Body.TransactionIds.Count - offset);
                var transactionIds = block.Body.TransactionIds.ToList().GetRange(offset, limit);
                foreach (var hash in transactionIds)
                {
                    var transactionResult = await this.GetTransactionResult(hash);
                    var jObjectResult = (JObject) JsonConvert.DeserializeObject(transactionResult.ToString());
                    var transaction = await TransactionManager.GetTransaction(transactionResult.TransactionId);
                    jObjectResult["BlockHash"] = block.GetHash().ToHex();

                    if (transactionResult.Status == TransactionResultStatus.Failed)
                        jObjectResult["Error"] = transactionResult.Error;

                    jObjectResult["Transaction"] = (JObject) JsonConvert.DeserializeObject(transaction.ToString());
                    jObjectResult["Status"] = transactionResult.Status.ToString();
                    response.Add(jObjectResult);
                }
            }

            return JArray.FromObject(response);
        }

        [JsonRpcMethod("GetBlockHeight")]
        public async Task<long> GetBlockHeight()
        {
            return await this.GetCurrentChainHeight();
        }

        [JsonRpcMethod("GetBlockInfo", "blockHeight", "includeTransactions")]
        public async Task<JObject> GetBlockInfo(long blockHeight, bool includeTransactions = false)
        {
            var blockInfo = await this.GetBlockAtHeight(blockHeight);
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
                    ["Extra"] = blockInfo.Header.ExtraData.ToString(),
                    ["Height"] = blockInfo.Header.Height.ToString(),
                    ["Time"] = blockInfo.Header.Time.ToDateTime(),
                    ["ChainId"] = ChainHelper.ConvertChainIdToBase58(blockInfo.Header.ChainId),
                    ["Bloom"] = blockInfo.Header.Bloom.ToByteArray().ToHex()
                },
                ["Body"] = new JObject
                {
                    ["TransactionsCount"] = blockInfo.Body.TransactionsCount,
                }
            };

            if (includeTransactions)
            {
                var transactions = blockInfo.Body.TransactionIds;
                var txs = new List<string>();
                foreach (var transactionId in transactions)
                {
                    txs.Add(transactionId.ToHex());
                }

                response["Body"]["Transactions"] = JArray.FromObject(txs);
            }

            return response;
        }

        [JsonRpcMethod("GetTransactionPoolStatus")]
        public async Task<JObject> GetTransactionPoolStatus()
        {
            return await this.GetTransactionPoolStatusAsync();
        }

        [JsonRpcMethod("GetChainStatus")]
        public async Task<JObject> GetChainStatus()
        {
            var chain = await BlockchainService.GetChainAsync();
            var branches = (JObject) JsonConvert.DeserializeObject(chain.Branches.ToString());
            var formattedNotLinkedBlocks = new JArray();

            foreach (var notLinkedBlock in chain.NotLinkedBlocks)
            {
                var block = await this.GetBlock(HashHelper.Base64ToHash(notLinkedBlock.Value));
                formattedNotLinkedBlocks.Add(new JObject
                    {
                        ["BlockHash"] = block.GetHash().ToHex(),
                        ["Height"] = block.Height,
                        ["PreviousBlockHash"] = block.Header.PreviousBlockHash.ToHex()
                    }
                );
            }

            return new JObject
            {
                ["Branches"] = branches,
                ["NotLinkedBlocks"] = formattedNotLinkedBlocks,
                ["LongestChainHeight"] = chain.LongestChainHeight,
                ["LongestChainHash"] = chain.LongestChainHash?.ToHex(),
                ["GenesisBlockHash"] = chain.GenesisBlockHash.ToHex(),
                ["LastIrreversibleBlockHash"] = chain.LastIrreversibleBlockHash?.ToHex(),
                ["LastIrreversibleBlockHeight"] = chain.LastIrreversibleBlockHeight,
                ["BestChainHash"] = chain.BestChainHash?.ToHex(),
                ["BestChainHeight"] = chain.BestChainHeight
            };
        }

        [JsonRpcMethod("GetBlockState", "blockHash")]
        public async Task<JObject> GetBlockState(string blockHash)
        {
            var stateStorageKey = HashHelper.HexStringToHash(blockHash).ToStorageKey();
            var blockState = await BlockStateSets.GetAsync(stateStorageKey);
            
            if (blockState == null)
                throw new JsonRpcServiceException(Error.NotFound, Error.Message[Error.NotFound]);
            
            return new JObject
            {
                ["BlockHash"] = blockState.BlockHash.ToHex(),
                ["BlockHeight"] = blockState.BlockHeight,
                ["Changes"] = (JObject) JsonConvert.DeserializeObject(blockState.Changes.ToString())
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
            Hash transactionId;
            try
            {
                transactionId = Hash.LoadHex(transactionId);
            }
            catch
            {
                throw new JsonRpcServiceException(Error.InvalidTransactionId, Error.Message[Error.InvalidTransactionId]);
            }

            var transactionResult = await this.GetTransactionResult(transactionId);
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Domain;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContractExecution.Domain;
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
        public ITxHub TxHub { get; set; }
        public ITransactionResultQueryService TransactionResultQueryService { get; set; }
        public ITransactionManager TransactionManager { get; set; }
        public ISmartContractExecutiveService SmartContractExecutiveService { get; set; }
        public IBinaryMerkleTreeManager BinaryMerkleTreeManager { get; set; }
        
        
        public ISmartContractAddressService SmartContractAddressService { get; set; }
        public IStateStore<BlockStateSet> BlockStateSets { get; set; }
        public ILogger<ChainControllerRpcService> Logger { get; set; }

        private readonly ChainOptions _chainOptions;
        public ILocalEventBus LocalEventBus { get; set; } = NullLocalEventBus.Instance;

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
            var basicContractZero = SmartContractAddressService.GetZeroSmartContractAddress();

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

            IMessage abi;
            try
            {
                abi = await this.GetContractAbi(addressHash);
            }
            catch
            {
                throw new JsonRpcServiceException(Error.NotFound, Error.Message[Error.NotFound]);
            }

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
                response = await this.CallReadOnly(transaction);
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
            Hash transactionHash;
            try
            {
                transactionHash = Hash.LoadHex(transactionId);
            }
            catch
            {
                throw new JsonRpcServiceException(Error.InvalidTransactionId, Error.Message[Error.InvalidTransactionId]);
            }

            return await BuildTransactionResult(transactionHash);
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
                realBlockHash = Hash.LoadHex(blockHash);
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

            var transactions = new JArray();
            if (offset <= block.Body.Transactions.Count - 1)
            {
                limit = Math.Min(limit, block.Body.Transactions.Count - offset);
                var transactionHashes = block.Body.Transactions.ToList().GetRange(offset, limit);
                foreach (var hash in transactionHashes)
                {
                    transactions.Add(await BuildTransactionResult(hash));
                }
            }

            return JArray.FromObject(transactions);
        }

        private async Task<JObject> BuildTransactionResult(Hash transactionHash)
        {
            var transactionResult = await this.GetTransactionResult(transactionHash);
            var response = (JObject) JsonConvert.DeserializeObject(transactionResult.ToString());
            var transaction = await this.TransactionManager.GetTransaction(transactionHash);
            response["Transaction"] = (JObject) JsonConvert.DeserializeObject(transaction.ToString());
            return response;
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
                    ["SideChainTransactionsRoot"] = blockInfo.Header.BlockExtraDatas.Any()
                        ? Hash.LoadByteArray(blockInfo.Header.BlockExtraDatas?[0].ToByteArray())?.ToHex()
                        : "",
                    ["Height"] = blockInfo.Header.Height.ToString(),
                    ["Time"] = blockInfo.Header.Time.ToDateTime(),
                    ["ChainId"] = ChainHelpers.ConvertChainIdToBase58(blockInfo.Header.ChainId),
                    ["Bloom"] = blockInfo.Header.Bloom.ToByteArray().ToHex()
                },
                ["Body"] = new JObject
                {
                    ["TransactionsCount"] = blockInfo.Body.TransactionsCount,
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
                var block = await this.GetBlock(Hash.LoadHex(notLinkedBlock.Value));
                formattedNotLinkedBlocks.Add(new JObject
                    {
                        ["BlockHash"] = block.BlockHashToHex,
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
            var blockState = await BlockStateSets.GetAsync(blockHash);
            if (blockState == null)
                throw new JsonRpcServiceException(Error.NotFound, Error.Message[Error.NotFound]);
            return JObject.FromObject(JsonConvert.DeserializeObject(blockState.ToString()));
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
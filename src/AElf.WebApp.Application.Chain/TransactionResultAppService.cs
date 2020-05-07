using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using AElf.WebApp.Application.Chain.Dto;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.Application.Chain
{
    public interface ITransactionResultAppService : IApplicationService
    {
        Task<TransactionResultDto> GetTransactionResultAsync(string transactionId);

        Task<List<TransactionResultDto>> GetTransactionResultsAsync(string blockHash, int offset = 0,
            int limit = 10);

        Task<MerklePathDto> GetMerklePathByTransactionIdAsync(string transactionId);
    }

    [ControllerName("BlockChain")]
    public class TransactionResultAppService : ITransactionResultAppService
    {
        private readonly ITransactionResultProxyService _transactionResultProxyService;
        private readonly ITransactionManager _transactionManager;
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;

        public TransactionResultAppService(ITransactionResultProxyService transactionResultProxyService,
            ITransactionManager transactionManager,
            IBlockchainService blockchainService,
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService)
        {
            _transactionResultProxyService = transactionResultProxyService;
            _transactionManager = transactionManager;
            _blockchainService = blockchainService;
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
        }

        /// <summary>
        /// Get the current status of a transaction
        /// </summary>
        /// <param name="transactionId">transaction id</param>
        /// <returns></returns>
        public async Task<TransactionResultDto> GetTransactionResultAsync(string transactionId)
        {
            Hash transactionIdHash;
            try
            {
                transactionIdHash = Hash.LoadFromHex(transactionId);
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidTransactionId],
                    Error.InvalidTransactionId.ToString());
            }

            var transactionResult = await GetTransactionResultAsync(transactionIdHash);
            var transaction = await _transactionManager.GetTransactionAsync(transactionResult.TransactionId);

            var output = JsonConvert.DeserializeObject<TransactionResultDto>(transactionResult.ToString());

            if (transactionResult.Status == TransactionResultStatus.NotExisted)
            {
                output.Status = transactionResult.Status.ToString();
                return output;
            }

            output.Transaction = JsonConvert.DeserializeObject<TransactionDto>(transaction.ToString());
            output.TransactionSize = transaction.CalculateSize();
            
            var methodDescriptor = await ContractMethodDescriptorHelper.GetContractMethodDescriptorAsync(
                _blockchainService, _transactionReadOnlyExecutionService, transaction.To, transaction.MethodName, false);

            if (methodDescriptor != null)
            {
                var parameters = methodDescriptor.InputType.Parser.ParseFrom(transaction.Params);
                if (!IsValidMessage(parameters))
                {
                    throw new UserFriendlyException(Error.Message[Error.InvalidParams], Error.InvalidParams.ToString());
                }

                output.Transaction.Params = JsonFormatter.ToDiagnosticString(parameters);
            }

            if (transactionResult.Status == TransactionResultStatus.Pending)
            {
                return output;
            }
            var block = await _blockchainService.GetBlockAtHeightAsync(transactionResult.BlockNumber);
            output.BlockHash = block.GetHash().ToHex();

            if (transactionResult.Status == TransactionResultStatus.Mined)
            {
                output.ReturnValue = transactionResult.ReturnValue.ToHex();
                var bloom = transactionResult.Bloom;
                output.Bloom = bloom.Length == 0 ? ByteString.CopyFrom(new byte[256]).ToBase64() : bloom.ToBase64();
            }

            if (transactionResult.Status == TransactionResultStatus.Failed)
            {
                output.Error = transactionResult.Error;
            }

            return output;
        }

        /// <summary>
        /// Get multiple transaction results.
        /// </summary>
        /// <param name="blockHash">block hash</param>
        /// <param name="offset">offset</param>
        /// <param name="limit">limit</param>
        /// <returns></returns>
        /// <exception cref="UserFriendlyException"></exception>
        public async Task<List<TransactionResultDto>> GetTransactionResultsAsync(string blockHash, int offset = 0,
            int limit = 10)
        {
            if (offset < 0)
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidOffset], Error.InvalidOffset.ToString());
            }

            if (limit <= 0 || limit > 100)
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidLimit], Error.InvalidLimit.ToString());
            }

            Hash realBlockHash;
            try
            {
                realBlockHash = Hash.LoadFromHex(blockHash);
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidBlockHash],
                    Error.InvalidBlockHash.ToString());
            }

            var block = await _blockchainService.GetBlockAsync(realBlockHash);
            if (block == null)
            {
                throw new UserFriendlyException(Error.Message[Error.NotFound], Error.NotFound.ToString());
            }

            var output = new List<TransactionResultDto>();
            if (offset <= block.Body.TransactionIds.Count - 1)
            {
                limit = Math.Min(limit, block.Body.TransactionIds.Count - offset);
                var transactionIds = block.Body.TransactionIds.ToList().GetRange(offset, limit);
                foreach (var transactionId in transactionIds)
                {
                    var transactionResultDto = await GetTransactionResultDto(transactionId, realBlockHash, block.GetHash());
                    output.Add(transactionResultDto);
                }
            }

            return output;
        }

        /// <summary>
        /// Get the merkle path of a transaction.
        /// </summary>
        /// <param name="transactionId"></param>
        /// <returns></returns>
        public async Task<MerklePathDto> GetMerklePathByTransactionIdAsync(string transactionId)
        {
            Hash transactionIdHash;
            try
            {
                transactionIdHash = Hash.LoadFromHex(transactionId);
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidTransactionId],
                    Error.InvalidTransactionId.ToString());
            }

            var transactionResult = await GetMinedTransactionResultAsync(transactionIdHash);
            var blockHash = transactionResult.BlockHash;
            var blockInfo = await _blockchainService.GetBlockByHashAsync(blockHash);
            var transactionIds = blockInfo.Body.TransactionIds;
            var index = transactionIds.IndexOf(transactionIdHash);
            if (index == -1)
            {
                throw new UserFriendlyException(Error.Message[Error.NotFound], Error.NotFound.ToString());
            }
            var leafNodes = await GetLeafNodesAsync(blockInfo.TransactionIds);

            var binaryMerkleTree = BinaryMerkleTree.FromLeafNodes(leafNodes);
            var path = binaryMerkleTree.GenerateMerklePath(index);
            var merklePath = new MerklePathDto {MerklePathNodes = new List<MerklePathNodeDto>()};
            foreach (var node in path.MerklePathNodes)
            {
                merklePath.MerklePathNodes.Add(new MerklePathNodeDto
                {
                    Hash = node.Hash.ToHex(), IsLeftChildNode = node.IsLeftChildNode
                });
            }

            return merklePath;
        }

        private async Task<TransactionResult> GetTransactionResultAsync(Hash transactionId, Hash blockHash = null)
        {
            // in tx pool
            var queuedTransaction = await _transactionResultProxyService.TxHub.GetQueuedTransactionAsync(transactionId);
            if (queuedTransaction != null)
            {
                return new TransactionResult
                {
                    TransactionId = queuedTransaction.TransactionId,
                    Status = TransactionResultStatus.Pending
                };
            }

            // in storage
            TransactionResult result;
            if (blockHash != null)
            {
                result =
                    await _transactionResultProxyService.TransactionResultQueryService.GetTransactionResultAsync(
                        transactionId, blockHash);
            }
            else
            {
                result =
                    await _transactionResultProxyService.TransactionResultQueryService.GetTransactionResultAsync(
                        transactionId);
            }

            if (result != null)
            {
                return result;
            }

            // not existed
            return new TransactionResult
            {
                TransactionId = transactionId,
                Status = TransactionResultStatus.NotExisted
            };
        }
        
        private async Task<TransactionResultDto> GetTransactionResultDto(Hash transactionId, Hash realBlockHash, Hash blockHash)
        {
            var transactionResult = await GetTransactionResultAsync(transactionId, realBlockHash);
            var transactionResultDto =
                JsonConvert.DeserializeObject<TransactionResultDto>(transactionResult.ToString());
            var transaction = await _transactionManager.GetTransactionAsync(transactionResult.TransactionId);
            transactionResultDto.BlockHash = blockHash.ToHex();
            transactionResultDto.ReturnValue = transactionResult.ReturnValue.ToHex();

            if (transactionResult.Status == TransactionResultStatus.Failed)
                transactionResultDto.Error = transactionResult.Error;

            transactionResultDto.Transaction =
                JsonConvert.DeserializeObject<TransactionDto>(transaction.ToString());
            transactionResultDto.TransactionSize = transaction.CalculateSize();

            var methodDescriptor =
                await ContractMethodDescriptorHelper.GetContractMethodDescriptorAsync(_blockchainService,
                    _transactionReadOnlyExecutionService, transaction.To, transaction.MethodName, false);

            if (methodDescriptor != null)
            {
                var parameters = methodDescriptor.InputType.Parser.ParseFrom(transaction.Params);
                if (!IsValidMessage(parameters))
                {
                    throw new UserFriendlyException(Error.Message[Error.InvalidParams], Error.InvalidParams.ToString());
                }

                transactionResultDto.Transaction.Params = JsonFormatter.ToDiagnosticString(parameters);
            }

            transactionResultDto.Status = transactionResult.Status.ToString();

            return transactionResultDto;
        }

        private async Task<TransactionResult> GetMinedTransactionResultAsync(Hash transactionIdHash)
        {
            var transactionResult = await GetTransactionResultAsync(transactionIdHash);
            switch (transactionResult.Status)
            {
                case TransactionResultStatus.Mined:
                {
                    var block = await _blockchainService.GetBlockAtHeightAsync(transactionResult.BlockNumber);
                    transactionResult.BlockHash = block.GetHash();
                    break;
                }
                case TransactionResultStatus.Failed:
                case TransactionResultStatus.NotExisted:
                    throw new UserFriendlyException(Error.Message[Error.NotFound], Error.NotFound.ToString());
            }

            return transactionResult;
        }

        private async Task<List<Hash>> GetLeafNodesAsync(IEnumerable<Hash> transactionIds)
        {
            var transactionResultList = new List<TransactionResult>();
            foreach (var item in transactionIds)
            {
                var result = await GetTransactionResultAsync(item);
                transactionResultList.Add(result);
            }

            var transactionResultSet = transactionResultList.Select(txResult => (txResult.TransactionId, txResult.Status));
            var leafNodes = new List<Hash>();
            foreach (var (txId, status) in transactionResultSet)
            {
                leafNodes.Add(GetHashCombiningTransactionAndStatus(txId, status));
            }

            return leafNodes;
        }

        private Hash GetHashCombiningTransactionAndStatus(Hash txId,
            TransactionResultStatus executionReturnStatus)
        {
            // combine tx result status
            var rawBytes = txId.ToByteArray().Concat(Encoding.UTF8.GetBytes(executionReturnStatus.ToString()))
                .ToArray();
            return HashHelper.ComputeFrom(rawBytes);
        }
        
        private bool IsValidMessage(IMessage message)
        {
            try
            {
                JsonFormatter.ToDiagnosticString(message);
            }
            catch
            {
                return false;
            }

            return true;
        }
    }
}
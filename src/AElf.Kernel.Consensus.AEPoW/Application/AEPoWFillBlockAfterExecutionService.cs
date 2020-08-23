using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;

namespace AElf.Kernel.Consensus.AEPoW.Application
{
    public class AEPoWFillBlockAfterExecutionService : FillBlockAfterExecutionService
    {
        private readonly INonceProvider _nonceProvider;

        public AEPoWFillBlockAfterExecutionService(INonceProvider nonceProvider)
        {
            _nonceProvider = nonceProvider;
        }

        public override Task<Block> FillAsync(BlockHeader header, IEnumerable<Transaction> transactions,
            ExecutionReturnSetCollection executionReturnSetCollection,
            BlockStateSet blockStateSet)
        {
            var block = base.FillAsync(header, transactions, executionReturnSetCollection, blockStateSet);
            
            // Provide block information to Web API.
            
            // Enqueue a task to calculate nonce in kernel.
            
            while (true)
            {
                // Calculate nonce.
                if (IsNonceReady(new BlockIndex {BlockHash = header.GetHash(), BlockHeight = header.Height}, out var nonce))
                {
                    break;
                }
            }
            
            // Set nonce to block header extra data.

            return block;
        }

        private bool IsNonceReady(BlockIndex blockIndex, out BigInteger nonce)
        {
            nonce = _nonceProvider.GetNonce(blockIndex);
            return nonce != default(BigInteger);
        }
    }
}
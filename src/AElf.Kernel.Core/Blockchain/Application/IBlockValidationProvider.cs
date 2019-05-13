using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using AElf.Kernel.Account.Application;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.Blockchain.Application
{
    public interface IBlockValidationProvider
    {
        Task<bool> ValidateBeforeAttachAsync(IBlock block);
        Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block);
        Task<bool> ValidateBlockAfterExecuteAsync(IBlock block);
    }


    [Serializable]
    public class BlockValidationException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public BlockValidationException()
        {
        }

        public BlockValidationException(string message) : base(message)
        {
        }

        public BlockValidationException(string message, Exception inner) : base(message, inner)
        {
        }

        protected BlockValidationException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public class ValidateNextTimeBlockValidationException : BlockValidationException
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public ValidateNextTimeBlockValidationException()
        {
        }

        public ValidateNextTimeBlockValidationException(string message) : base(message)
        {
        }

        public ValidateNextTimeBlockValidationException(string message, Exception inner) : base(message, inner)
        {
        }

        public ValidateNextTimeBlockValidationException(Hash blockhash) : this(
            $"validate next time, block hash = {blockhash.ToHex()}")
        {
            BlockHash = blockhash;
        }

        protected ValidateNextTimeBlockValidationException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }

        public Hash BlockHash { get; private set; }
    }

    public class BlockValidationProvider : IBlockValidationProvider
    {
        private IBlockchainService _blockchainServce;
        public ILogger<BlockValidationProvider> Logger { get; set; }

        public BlockValidationProvider(IBlockchainService blockchainService)
        {
            _blockchainServce = blockchainService;
        }

        public async Task<bool> ValidateBeforeAttachAsync(IBlock block)
        {
            if (block?.Header == null || block.Body == null)
            {
                Logger.LogWarning($"Block header or body is null {block}");
                return false;
            }

            if (block.Body.TransactionsCount == 0)
            {
                Logger.LogWarning($"Block transactions is empty");
                return false;
            }

            if (_blockchainServce.GetChainId() != block.Header.ChainId)
            {
                Logger.LogWarning($"Block chain id mismatch {block.Header.ChainId}");
                return false;
            }

            if (block.Header.Height != KernelConstants.GenesisBlockHeight && !block.VerifySignature())
            {
                Logger.LogWarning($"Block verify signature failed. {block}");
                return false;
            }

            if (block.Body.CalculateMerkleTreeRoots() != block.Header.MerkleTreeRootOfTransactions)
            {
                Logger.LogWarning($"Block merkle tree root mismatch {block}");
                return false;
            }

            if (block.Header.Height != KernelConstants.GenesisBlockHeight &&
                block.Header.Time.ToDateTime() - DateTime.UtcNow > KernelConsts.AllowedFutureBlockTimeSpan)
            {
                Logger.LogWarning($"Future block received {block}, {block.Header.Time.ToDateTime()}");
                return false;
            }

            return true;
        }

        public async Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block)
        {
            if (block?.Header == null || block.Body == null)
                return false;

            if (block.Body.TransactionsCount == 0)
                return false;

            return true;
        }

        public async Task<bool> ValidateBlockAfterExecuteAsync(IBlock block)
        {
            return true;
        }
    }
}
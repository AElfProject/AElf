using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Consensus.Application
{
    public interface IConsensusService
    {
        /// <summary>
        /// To trigger the consensus scheduler at the starting of current node.
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        Task TriggerConsensusAsync(int chainId);
        
        /// <summary>
        /// To validate the consensus information extracted from block header extra data.
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="preBlockHash"></param>
        /// <param name="preBlockHeight"></param>
        /// <param name="consensusExtraData">Extract from block header.</param>
        /// <returns></returns>
        Task<bool> ValidateConsensusAsync(int chainId, Hash preBlockHash, ulong preBlockHeight,
            byte[] consensusExtraData);
        
        /// <summary>
        /// After the execution of consensus transactions, the new consensus
        /// information will emerge from consensus contract.
        /// The consensus information will used to fill block header extra
        /// data of new block.
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        Task<byte[]> GetNewConsensusInformationAsync(int chainId);
        
        /// <summary>
        /// Generate consensus transactions from consensus contract.
        /// </summary>
        /// <param name="chainId"></param>
        /// <returns></returns>
        Task<IEnumerable<Transaction>> GenerateConsensusTransactionsAsync(int chainId);
    }
}
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
        /// <returns></returns>
        Task TriggerConsensusAsync(ChainContext chainContext);

        /// <summary>
        /// To validate the consensus information extracted from block header extra data.
        /// </summary>
        /// <param name="chainContext"></param>
        /// <param name="consensusExtraData">Extract from block header.</param>
        /// <returns></returns>
        Task<bool> ValidateConsensusBeforeExecutionAsync(ChainContext chainContext,
            byte[] consensusExtraData);

        /// <summary>
        /// To validate the consensus information extracted from block header extra data.
        /// </summary>
        /// <param name="chainContext"></param>
        /// <param name="consensusExtraData">Extract from block header.</param>
        /// <returns></returns>
        Task<bool> ValidateConsensusAfterExecutionAsync(ChainContext chainContext,
            byte[] consensusExtraData);

        /// <summary>
        /// After the execution of consensus transactions, the new consensus
        /// information will emerge from consensus contract.
        /// The consensus information will used to fill block header extra
        /// data of new block.
        /// </summary>
        /// <returns></returns>
        Task<byte[]> GetInformationToUpdateConsensusAsync(ChainContext chainContext);

        /// <summary>
        /// Generate consensus transactions from consensus contract.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<Transaction>> GenerateConsensusTransactionsAsync(ChainContext chainContext);
    }
}
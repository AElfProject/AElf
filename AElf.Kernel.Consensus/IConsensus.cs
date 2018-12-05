using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common.Enums;
using AElf.Common;
using Google.Protobuf;

namespace AElf.Kernel.Consensus
{
    /// <summary>
    /// This is a common interface of all consensus protocols.
    /// </summary>
    public interface IConsensus
    {
        /// <summary>
        /// The type of consensus protocol.
        /// </summary>
        ConsensusType Type { get; }
        
        /// <summary>
        /// For DPoS, this value should start from 1, and will
        /// increase 1 after each round.
        /// For other consensus protocol, this should always be 1.
        /// </summary>
        ulong CurrentRoundNumber { get; }

        /// <summary>
        /// How soon to produce a block.
        /// (Milliseconds)
        /// </summary>
        int Interval { get; }
        
        /// <summary>
        /// Print logs or not.
        /// </summary>
        int LogLevel { get; set; }

        /// <summary>
        /// To adjust the difficulty of PoW mining.
        /// </summary>
        Hash Nonce { get; set; }
        
        /// <summary>
        /// For AElf DPoS, this method is used for publishing the
        /// information of first two rounds (by Chain Creator).
        /// </summary>
        /// <returns></returns>
        Task Initialize(List<byte[]> args);

        /// <summary>
        /// For AElf DPoS, this method is used for publishing the
        /// information of next round (by Extra Block Producer).
        /// </summary>
        /// <returns></returns>
        Task Update(List<byte[]> args);

        /// <summary>
        /// For AElf DPoS, this method is used for publishing the
        /// out value, signature or in value (by every Block Producer).
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        Task Publish(List<byte[]> args);

        /// <summary>
        /// Used by ConsensusValidationFilter,
        /// to check the received block is produced correctly
        /// or not.
        /// </summary>
        /// <returns></returns>
        Task<int> Validation(List<byte[]> args);

        Task Election(List<byte[]> args);

        Miners GetCurrentMiners();
        Task HandleTickets(Address address, ulong amount, bool withdraw = false);
    }
}
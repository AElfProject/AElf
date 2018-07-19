using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Sdk.CSharp.Types;

namespace AElf.Contracts.Consensus
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
        /// For DPoS, this value should increase 1 after each round,
        /// for other consensus protocol, this should always be 1.
        /// </summary>
        UInt64Field CurrentRoundNumber { get; set; }

        Task InitializeConsensus();
        
        
    }
}
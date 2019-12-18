// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.AEDPoS
{
    /// <summary>
    /// Useful data for validating consensus header information.
    /// </summary>
    public class ConsensusValidationContext
    {
        public long CurrentTermNumber { get; set; }
        public long CurrentRoundNumber { get; set; }

        /// <summary>
        /// We can trust this because we already validated the pubkey
        /// during `AEDPoSExtraDataExtractor.ExtractConsensusExtraData`
        /// </summary>
        public string SenderPubkey => ExtraData.SenderPubkey.ToHex();

        /// <summary>
        /// Round information fetch from StateDb.
        /// </summary>
        public Round BaseRound { get; set; }

        /// <summary>
        /// Round information included in the consensus header extra data.
        /// </summary>
        public Round ProvidedRound => ExtraData.Round;

        /// <summary>
        /// Previous round information fetch from StateDb.
        /// </summary>
        public Round PreviousRound { get; set; }

        /// <summary>
        /// This filed is to prevent one miner produces too many continues blocks
        /// (which may cause problems to other parts).
        /// </summary>
        public LatestPubkeyToTinyBlocksCount LatestPubkeyToTinyBlocksCount { get; set; }

        public AElfConsensusHeaderInformation ExtraData { get; set; }
    }
}
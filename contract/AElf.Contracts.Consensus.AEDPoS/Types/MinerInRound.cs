namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class MinerInRound
    {
        /// <summary>
        /// If one miner produced tiny blocks due to he's the extra block producer of previous round,
        /// we can't say he mined block `for` current round.
        /// </summary>
        /// <returns></returns>
        public bool IsMinedBlockForCurrentRound()
        {
            return OutValue != null;
        }
    }
}
namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class MinerInRound
    {
        /// <summary>
        /// If one miner is producing tiny blocks for the extra block time slot of previous round,
        /// we can still say current round is a new round for him.
        /// </summary>
        /// <returns></returns>
        public bool IsThisANewRoundForThisMiner()
        {
            return OutValue != null;
        }
    }
}
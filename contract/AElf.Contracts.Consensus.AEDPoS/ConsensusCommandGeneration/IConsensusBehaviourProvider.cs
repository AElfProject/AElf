namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        public interface IConsensusBehaviourProvider
        {
            AElfConsensusBehaviour GetConsensusBehaviour();
        }
    }
}
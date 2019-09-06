// ReSharper disable once CheckNamespace
namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        internal interface IConsensusBehaviourProvider
        {
            AElfConsensusBehaviour GetConsensusBehaviour();
        }
    }
}
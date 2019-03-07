using AElf.Kernel;

namespace AElf.Sdk.CSharp
{
    // ReSharper disable InconsistentNaming
    public interface IMainChainDPoSConsensusSmartContract
    {
        void InitialDPoS(Round firstRound);
        void UpdateValue(ToUpdate toUpdate);
        void NextRound(Round round);
        void NextTerm(Round round);
    }
    
    public interface ISideChainDPoSConsensusSmartContract
    {
        void InitialDPoS(Round firstRound);
        void UpdateValue(ToUpdate toUpdate);
        void NextRound(Round round);
    }
}
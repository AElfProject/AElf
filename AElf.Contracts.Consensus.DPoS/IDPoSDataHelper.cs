using AElf.Common;
using AElf.Kernel;

namespace AElf.Contracts.Consensus.DPoS
{
    // ReSharper disable once InconsistentNaming
    public interface IDPoSDataHelper
    {
        bool TryToUpdateRoundNumber(ulong roundNumber);
        bool TryToUpdateTermNumber(ulong termNumber);
        
        bool TryToGetRoundNumber(out ulong roundNumber);
        bool TryToGetTermNumber(out ulong termNumber);
        bool TryToGetCurrentRoundInformation(out Round roundInformation);
        bool TryToGetPreviousRoundInformation(out Round roundInformation);
        bool TryToGetMiners(ulong termNumber, out Miners miners);
        Miners TryToGetVictories();
        
        bool IsMiner(Address address);
    }
}
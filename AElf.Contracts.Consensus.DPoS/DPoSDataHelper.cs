using AElf.Common;
using AElf.Kernel;

namespace AElf.Contracts.Consensus.DPoS
{
    // ReSharper disable once InconsistentNaming
    public class DPoSDataHelper : IDPoSDataHelper
    {
        private readonly DataStructures _dataStructures;

        public DPoSDataHelper(DataStructures dataStructures)
        {
            _dataStructures = dataStructures;
        }

        public bool TryToUpdateRoundNumber(ulong roundNumber)
        {
            throw new System.NotImplementedException();
        }

        public bool TryToUpdateTermNumber(ulong termNumber)
        {
            throw new System.NotImplementedException();
        }
        
        public bool TryToGetRoundNumber(out ulong roundNumber)
        {
            throw new System.NotImplementedException();
        }

        public bool TryToGetTermNumber(out ulong termNumber)
        {
            throw new System.NotImplementedException();
        }

        public bool TryToGetCurrentRoundInformation(out Round roundInformation)
        {
            throw new System.NotImplementedException();
        }

        public bool TryToGetPreviousRoundInformation(out Round roundInformation)
        {
            throw new System.NotImplementedException();
        }

        public bool TryToGetMiners(ulong termNumber, out Miners miners)
        {
            throw new System.NotImplementedException();
        }

        public bool IsMiner(Address address)
        {
            if (TryToGetTermNumber(out var termNumber))
            {
                if (TryToGetMiners(termNumber, out var miners))
                {
                    return miners.Addresses.Contains(address);
                }
            }

            return false;
        }
        
        
        
        
    }
}
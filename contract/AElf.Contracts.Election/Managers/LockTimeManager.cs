using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types;

namespace AElf.Contracts.Election.Managers
{
    public class LockTimeManager : ILockTimeManager
    {
        private readonly MappedState<Hash, long> _lockTimeMap;

        public LockTimeManager(CSharpSmartContractContext context, MappedState<Hash, long> lockTimeMap)
        {
            _lockTimeMap = lockTimeMap;
        }

        public void SetLockTime(Hash voteId, long lockTime)
        {
            _lockTimeMap[voteId] = lockTime;
        }
    }
}
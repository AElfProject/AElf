using AElf.Types;

namespace AElf.Contracts.Election.Managers
{
    public interface ILockTimeManager
    {
        void SetLockTime(Hash voteId, long lockTime);
    }
}
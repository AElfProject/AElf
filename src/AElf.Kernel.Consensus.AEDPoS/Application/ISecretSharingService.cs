using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Types;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public interface ISecretSharingService
    {
        Task AddSharingInformationAsync(LogEvent logEvent);
        Dictionary<string, byte[]> GetEncryptedPieces(long roundId);
        Dictionary<string, byte[]> GetDecryptedPieces(long roundId);
        Dictionary<string, Hash> GetRevealedInValues(long roundId);
    }
}
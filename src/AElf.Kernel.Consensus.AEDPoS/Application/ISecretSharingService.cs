using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Types;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    internal interface ISecretSharingService
    {
        Task AddSharingInformationAsync(SecretSharingInformation secretSharingInformation);
        Dictionary<string, byte[]> GetEncryptedPieces(long roundId);
        Dictionary<string, byte[]> GetDecryptedPieces(long roundId);
        Dictionary<string, Hash> GetRevealedInValues(long roundId);
    }
}
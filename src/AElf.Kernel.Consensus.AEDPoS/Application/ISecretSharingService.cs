using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    internal interface ISecretSharingService
    {
        Task AddSharingInformationAsync(SecretSharingInformation secretSharingInformation);
        Dictionary<string, byte[]> GetSharingPieces(long roundId);
        Dictionary<string, byte[]> GetRevealedInValues(long roundId);
    }
}
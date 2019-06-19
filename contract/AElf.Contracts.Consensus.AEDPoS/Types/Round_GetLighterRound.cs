using System.Linq;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class Round
    {
        public void DeleteSecretSharingInformation()
        {
            var encryptedInValues = RealTimeMinersInformation.Values.Select(i => i.EncryptedInValues);
            foreach (var encryptedInValue in encryptedInValues)
            {
                encryptedInValue.Clear();
            }
            
            var decryptedInValues = RealTimeMinersInformation.Values.Select(i => i.DecryptedPreviousInValues);
            foreach (var decryptedInValue in decryptedInValues)
            {
                decryptedInValue.Clear();
            }
        }
    }
}
using System.Linq;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class Round
    {
        public void DeleteSecretSharingInformation()
        {
            var encryptedPieces = RealTimeMinersInformation.Values.Select(i => i.EncryptedPieces);
            foreach (var encryptedPiece in encryptedPieces)
            {
                encryptedPiece.Clear();
            }

            var decryptedPieces = RealTimeMinersInformation.Values.Select(i => i.DecryptedPieces);
            foreach (var decryptedPiece in decryptedPieces)
            {
                decryptedPiece.Clear();
            }
        }
    }
}
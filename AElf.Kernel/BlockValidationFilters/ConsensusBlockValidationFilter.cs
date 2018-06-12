using System.Linq;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Extensions;
using AElf.Kernel.Node.Config;
using AElf.Kernel.Services;
using Google.Protobuf;

namespace AElf.Kernel.BlockValidationFilters
{
    public class ConsensusBlockValidationFilter: IBlockValidationFilter
    {
        public Task<ValidationError> ValidateBlockAsync(IBlock block, IChainContext context)
        {
           
            // block signature
            var pubkey = block.Header.P.ToBase64();
            if (!MinersInfo.Instance.Producers.TryGetValue(pubkey, out var dict))
            {
                return Task.FromResult(ValidationError.InvalidBlcok);
            }
            
            byte[] uncompressedPrivKey = block.Header.P.ToByteArray();
            Hash addr = uncompressedPrivKey.CalculateHash().Take(ECKeyPair.AddressLength).ToArray();

            if (!addr.Equals(new Hash(ByteString.FromBase64(dict["coinbase"]))))
                return Task.FromResult(ValidationError.InvalidBlcok);
            
            ECKeyPair recipientKeyPair = ECKeyPair.FromPublicKey(uncompressedPrivKey);
            ECVerifier verifier = new ECVerifier(recipientKeyPair);
            if (!verifier.Verify(block.Header.GetSignature(), block.Header.GetHash().GetHashBytes()))
            {
                // verification failed
                return Task.FromResult(ValidationError.InvalidBlcok);
            }
            
            // todo: verify period for this producer
            var timestamp = block.Header.Time;
            
            var h = context.BlockHeight;

            return Task.FromResult(ValidationError.Success);

        }
    }
}
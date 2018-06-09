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
        public async Task<bool> ValidateBlockAsync(IBlock block, IChainContext context)
        {
            // block height
            if (block.Header.Index != context.BlockHeight)
                return false;

            // previous block hash
            if (!block.Header.PreviousBlockHash.Equals(context.BlockHash))
                return false;
            
            // block signature
            var pubkey = block.Header.P.ToBase64();
            if (!MinersInfo.Instance.Producers.TryGetValue(pubkey, out var dict))
            {
                return false;
            }
            
            byte[] uncompressedPrivKey = block.Header.P.ToByteArray();
            Hash addr = uncompressedPrivKey.CalculateHash().Take(ECKeyPair.AddressLength).ToArray();

            if (!addr.Equals(new Hash(ByteString.FromBase64(dict["coinbase"]))))
                return false;
            ECKeyPair recipientKeyPair = ECKeyPair.FromPublicKey(uncompressedPrivKey);
            ECVerifier verifier = new ECVerifier(recipientKeyPair);
            if (!verifier.Verify(block.Header.GetSignature(), block.Header.GetHash().GetHashBytes()))
                return false;
            
            // todo: verify the identity of producer
            var timestamp = block.Header.Time;
            
            var h = context.BlockHeight;

            return true;

        }
    }
}
using System.Linq;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Node.Config;
using AElf.Kernel.Services;
using AElf.Kernel.Types;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.BlockValidationFilters
{
    public class ConsensusBlockValidationFilter: IBlockValidationFilter
    {
        private readonly ISmartContractService _smartContractService;

        public ConsensusBlockValidationFilter(ISmartContractService smartContractService)
        {
            _smartContractService = smartContractService;
        }

        public async Task<ValidationError> ValidateBlockAsync(IBlock block, IChainContext context, ECKeyPair keyPair)
        {
            // block signature
            var pubkey = block.Header.P.ToBase64();
            if (!MinersInfo.Instance.Producers.TryGetValue(pubkey, out var dict))
            {
                return ValidationError.InvalidBlock;
            }
            
            byte[] uncompressedPrivKey = block.Header.P.ToByteArray();
            Hash addr = uncompressedPrivKey.CalculateHash().Take(ECKeyPair.AddressLength).ToArray();

            if (!addr.Equals(new Hash(ByteString.FromBase64(dict["coinbase"]))))
                return ValidationError.InvalidBlock;

            
            ECKeyPair recipientKeyPair = ECKeyPair.FromPublicKey(uncompressedPrivKey);
            ECVerifier verifier = new ECVerifier(recipientKeyPair);
            if (!verifier.Verify(block.Header.GetSignature(), block.Header.GetHash().GetHashBytes()))
            {
                // verification failed
                return ValidationError.InvalidBlock;
            }
            
            var contractAccountHash = new Hash(context.ChainId.CalculateHashWith("__SmartContractZero__")).ToAccount();
            var executive = await _smartContractService.GetExecutiveAsync(contractAccountHash, context.ChainId);
            var tx = GetTxToVerifyBlockProducer(contractAccountHash, keyPair);
            var tc = new TransactionContext
            {
                Transaction = tx
            };
            executive.SetTransactionContext(tc).Apply(true).Wait();
            if (!BoolValue.Parser.ParseFrom(tc.Trace.RetVal.ToByteArray()).Value)
            {
                return ValidationError.InvalidBlock;
            }
            
            return ValidationError.Success;
        }

        private ITransaction GetTxToVerifyBlockProducer(Hash contractAccountHash, ECKeyPair keyPair)
        {
            var tx = new Transaction
            {
                From = keyPair.GetAddress(),
                To = contractAccountHash,
                IncrementId = 0,
                MethodName = "IsBP",
                P = ByteString.CopyFrom(keyPair.PublicKey.Q.GetEncoded()),
                Params = ByteString.CopyFrom(ParamsPacker.Pack())
            };
            
            var signer = new ECSigner();
            var signature = signer.Sign(keyPair, tx.GetHash().GetHashBytes());

            // Update the signature
            tx.R = ByteString.CopyFrom(signature.R);
            tx.S = ByteString.CopyFrom(signature.S);

            return tx;
        }
    }
}
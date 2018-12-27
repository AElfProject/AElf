using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Managers;
using Google.Protobuf;

namespace AElf.SmartContract.Proposal
{
    // todo review byte array performance
    public class AuthorizationInfoReader : IAuthorizationInfoReader
    {
        private readonly ContractInfoReader _contractInfoReader;

        private Address AuthorizationContractAddress =>
            ContractHelpers.GetAuthorizationContractAddress(Hash.LoadByteArray(ChainConfig.Instance.ChainId.DecodeBase58()));
        
        public AuthorizationInfoReader(IStateManager stateManager)
        {
            var chainId = Hash.LoadBase58(ChainConfig.Instance.ChainId);
            _contractInfoReader = new ContractInfoReader(chainId, stateManager);
        }
        
        // todo review
        public async Task<bool> CheckAuthority(Transaction transaction)
        {
            var sigCount = transaction.Sigs.Count;
            if (sigCount == 0)
                return false;
            
            // Get tx hash
            var hash = transaction.GetHash().DumpByteArray();

            if (transaction.Sigs.Count == 1)
                return true;
            // Get pub keys
            List<byte[]> publicKeys = new List<byte[]>();

            foreach (var sig in transaction.Sigs)
            {
                var pub = CryptoHelpers.RecoverPublicKey(sig.ToByteArray(), hash);
                publicKeys.Add(pub);
            }
            
            return await CheckAuthority(transaction.From, publicKeys);
        }

        public async Task<Kernel.Proposal> GetProposal(Hash proposalHash)
        {
            var bytes = await _contractInfoReader.GetBytesAsync<Authorization>(AuthorizationContractAddress,
                Hash.FromMessage(proposalHash), GlobalConfig.AElfProposal);
            return Kernel.Proposal.Parser.ParseFrom(bytes);
        }

        public async Task<Authorization> GetAuthorization(Address msig)
        {
            var bytes = await _contractInfoReader.GetBytesAsync<Authorization>(AuthorizationContractAddress,
                Hash.FromMessage(msig), GlobalConfig.AElfMultiSig);
            return bytes != null ? Authorization.Parser.ParseFrom(bytes) : new Authorization();
        }

        public async Task<bool> CheckAuthority(Address mSigAddress, IEnumerable<byte[]> pubKeys)
        {
            var auth = await GetAuthorization(mSigAddress);
            return CheckAuthority(auth, pubKeys);
        }

        private bool CheckAuthority(Authorization authorization, IEnumerable<byte[]> pubKeys)
        {
            var provided = pubKeys
                .Select(pubKey => authorization.Reviewers.FirstOrDefault(r => r.PubKey.Equals(pubKey)))
                .Where(r => r != null).Aggregate<Reviewer, long>(0, (current, r) => current + r.Weight);

            return provided >= authorization.ExecutionThreshold;
        }
    }
}
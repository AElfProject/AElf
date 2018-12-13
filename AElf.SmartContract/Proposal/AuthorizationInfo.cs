using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Storages;

namespace AElf.SmartContract.Proposal
{
    // todo review byte array performance
    public class AuthorizationInfo : IAuthorizationInfo
    {
        private readonly ContractInfoReader _contractInfoReader;

        private Address AuthorizationContractAddress =>
            ContractHelpers.GetAuthorizationContractAddress(Hash.LoadByteArray(ChainConfig.Instance.ChainId.DecodeBase58()));
        
        public AuthorizationInfo(IStateStore stateStore)
        {
            var chainId = Hash.LoadBase58(ChainConfig.Instance.ChainId);
            _contractInfoReader = new ContractInfoReader(chainId, stateStore);
        }
        
        // todo review
        public bool CheckAuthority(Transaction transaction)
        {
            var sigCount = transaction.Sigs.Count;
            if (sigCount == 0)
                return false;
            
            // Get tx hash
            var hash = transaction.GetHash().DumpByteArray();

            if (transaction.Sigs.Count == 1)
                return true;

            // Get pub keys
            List<byte[]> publicKey = new List<byte[]>(transaction.Sigs.Count);
            for (int i = 0; i < transaction.Sigs.Count; i++)
            {
                publicKey[i] = CryptoHelpers.RecoverPublicKey(transaction.Sigs[i].ToByteArray(), hash);
            }

            return CheckAuthority(transaction.From, publicKey);
        }

        public Kernel.Proposal GetProposal(Hash proposalHash)
        {
            var bytes = _contractInfoReader.GetBytes<Authorization>(AuthorizationContractAddress,
                Hash.FromMessage(proposalHash), GlobalConfig.AElfProposal);
            return Kernel.Proposal.Parser.ParseFrom(bytes);
        }

        public Authorization GetAuthorization(Address msig)
        {
            var bytes = _contractInfoReader.GetBytes<Authorization>(AuthorizationContractAddress,
                Hash.FromMessage(msig), GlobalConfig.AElfTxRootMerklePathInParentChain);
            return Authorization.Parser.ParseFrom(bytes);
        }

        public bool CheckAuthority(Address mSigAddress, IEnumerable<byte[]> pubKeys)
        {
            var auth = GetAuthorization(mSigAddress);
            return CheckAuthority(auth, pubKeys);
        }

        private bool CheckAuthority(Authorization authorization, IEnumerable<byte[]> pubKeys)
        {
            long provided = pubKeys
                .Select(pubKey => authorization.Reviewers.FirstOrDefault(r => r.PubKey.Equals(pubKey)))
                .Where(r => r != null).Aggregate<Reviewer, long>(0, (current, r) => current + r.Weight);

            return provided >= authorization.ExecutionThreshold;
        }
    }
}
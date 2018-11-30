using System.Collections.Generic;
using System.Linq;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Kernel;
using AElf.Kernel.Storages;
using AElf.Kernel.Types.Proposal;
using Google.Protobuf;

namespace AElf.SmartContract.Proposal
{
    public class AuthorizationInfo : IAuthorizationInfo
    {
        private readonly ContractInfoReader _contractInfoReader;
        private Address AuthorizationContractAddress =>
            ContractHelpers.GetAuthorizationContractAddress(Hash.LoadHex(ChainConfig.Instance.ChainId));
        public AuthorizationInfo(IStateStore stateStore)
        {
            var chainId = Hash.LoadHex(ChainConfig.Instance.ChainId);
            _contractInfoReader = new ContractInfoReader(chainId, stateStore);
        }
        
        public bool CheckAuthority(Transaction transaction)
        {
            return transaction.Sigs.Count == 1 ||
                   CheckAuthority(transaction.From, transaction.Sigs.Select(sig => sig.P));
        }

        public Kernel.Types.Proposal.Proposal GetProposal(Hash proposalHash)
        {
            var bytes = _contractInfoReader.GetBytes<Authorization>(AuthorizationContractAddress,
                Hash.FromMessage(proposalHash), GlobalConfig.AElfProposal);
            return Kernel.Types.Proposal.Proposal.Parser.ParseFrom(bytes);
        }

        public Authorization GetAuthorization(Address msig)
        {
            var bytes = _contractInfoReader.GetBytes<Authorization>(AuthorizationContractAddress,
                Hash.FromMessage(msig), GlobalConfig.AElfTxRootMerklePathInParentChain);
            return Authorization.Parser.ParseFrom(bytes);
        }

        public bool CheckAuthority(Address mSigAddress, IEnumerable<ByteString> pubKeys)
        {
            var auth = GetAuthorization(mSigAddress);
            return CheckAuthority(auth, pubKeys);
        }

        private bool CheckAuthority(Authorization authorization, IEnumerable<ByteString> pubKeys)
        {
            long provided = pubKeys
                .Select(pubKey => authorization.Reviewers.FirstOrDefault(r => r.PubKey.Equals(pubKey)))
                .Where(r => r != null).Aggregate<Reviewer, long>(0, (current, r) => current + r.Weight);

            return provided >= authorization.ExecutionThreshold;
        }
    }
}
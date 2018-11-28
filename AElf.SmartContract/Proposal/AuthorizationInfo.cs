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
        private readonly ContractInfoReader _crossChainReader;
        private Address AuthorizationContractAddress =>
            ContractHelpers.GetAuthorizationContractAddress(Hash.LoadHex(ChainConfig.Instance.ChainId));
        public AuthorizationInfo(IStateStore stateStore)
        {
            var chainId = Hash.LoadHex(ChainConfig.Instance.ChainId);
            _crossChainReader = new ContractInfoReader(chainId, stateStore);
        }
        
        public bool CheckAuthority(Transaction transaction)
        {
            return transaction.Sigs.Count == 1 ||
                   CheckAuthority(transaction.From, transaction.Sigs.Select(sig => sig.P));
        }
        
        public bool CheckAuthority(Address mSigAddress, IEnumerable<ByteString> pubKeys)
        {
            var bytes = _crossChainReader.GetBytes<Authorization>(AuthorizationContractAddress,
                Hash.FromMessage(mSigAddress), GlobalConfig.AElfTxRootMerklePathInParentChain);
            var auth = Authorization.Parser.ParseFrom(bytes);
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
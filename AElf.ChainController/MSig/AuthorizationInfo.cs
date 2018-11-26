using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Kernel.Storages;
using AElf.Kernel.Types.Auth;
using AElf.SmartContract;
using Google.Protobuf;

namespace AElf.ChainController.MSig
{
    public class AuthorizationInfo : IAuthorizationInfo
    {
        private readonly ContractInfoHelper _crossChainHelper;
        private Address AuthorizationContractAddress =>
            ContractHelpers.GetAuthorizationContractAddress(Hash.LoadHex(ChainConfig.Instance.ChainId));
        public AuthorizationInfo(IStateStore stateStore)
        {
            var chainId = Hash.LoadHex(ChainConfig.Instance.ChainId);
            _crossChainHelper = new ContractInfoHelper(chainId, stateStore);
        }
        public bool CheckAuthority(Address mSigAddress, IEnumerable<ByteString> pubKeys)
        {
            var bytes = _crossChainHelper.GetBytes<Auth>(AuthorizationContractAddress,
                Hash.FromMessage(mSigAddress), GlobalConfig.AElfTxRootMerklePathInParentChain);
            var auth = Auth.Parser.ParseFrom(bytes);
            return CheckAuthority(auth, pubKeys);
        }

        private bool CheckAuthority(Auth auth, IEnumerable<ByteString> pubKeys)
        {
            long provided = 0;
            foreach (var pubKey in pubKeys)
            {
                var p = auth.Reviewers.FirstOrDefault(r => r.PubKey.Equals(pubKey));
                if(p == null)
                    continue;
                provided += p.Weight;
            }

            return provided >= auth.ExecutionThreshold;
        }
    }
}
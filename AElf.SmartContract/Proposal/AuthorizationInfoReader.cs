using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration.Config.Chain;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Managers;
using AElf.Kernel.Types;

namespace AElf.SmartContract.Proposal
{
    // todo review byte array performance
    public class AuthorizationInfoReader : IAuthorizationInfoReader
    {
        private readonly ContractInfoReader _contractInfoReader;

        public AuthorizationInfoReader(IStateManager stateManager)
        {
            _contractInfoReader = new ContractInfoReader(stateManager);
        }

        public async Task<Kernel.Proposal> GetProposal(int chainId, Hash proposalHash)
        {
            var bytes = await _contractInfoReader.GetBytesAsync<Authorization>(chainId,
                ContractHelpers.GetAuthorizationContractAddress(chainId), Hash.FromMessage(proposalHash),
                GlobalConfig.AElfProposal);
            return Kernel.Proposal.Parser.ParseFrom(bytes);
        }

        public async Task<Authorization> GetAuthorization(int chainId, Address msig)
        {
            var bytes = await _contractInfoReader.GetBytesAsync<Authorization>(chainId,
                ContractHelpers.GetAuthorizationContractAddress(chainId), Hash.FromMessage(msig),
                GlobalConfig.AElfMultiSig);
            return bytes != null ? Authorization.Parser.ParseFrom(bytes) : new Authorization();
        }

        public async Task<bool> CheckAuthority(int chainId, Address mSigAddress, IEnumerable<byte[]> pubKeys)
        {
            var auth = await GetAuthorization(chainId, mSigAddress);
            return ValidateAuthorization(auth, pubKeys);
        }

        /// <summary>
        /// Check authorization validness.
        /// </summary>
        /// <param name="authorization">Authorization data.</param>
        /// <param name="pubKeys">Provided public keys.</param>
        /// <returns></returns>
        public bool ValidateAuthorization(Authorization authorization, IEnumerable<byte[]> pubKeys)
        {
            var enumerable = pubKeys as byte[][] ?? pubKeys.ToArray();
            var provided = enumerable
                .Select(pubKey =>
                    authorization.Reviewers.FirstOrDefault(r => r.PubKey.ToByteArray().SequenceEqual(pubKey)))
                .Where(r => !(r is default(Reviewer))).Aggregate<Reviewer, long>(0, (current, r) => current + r.Weight);

            return provided >= authorization.ExecutionThreshold;
        }
    }
}
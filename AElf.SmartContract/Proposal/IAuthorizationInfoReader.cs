using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using Google.Protobuf;

namespace AElf.SmartContract.Proposal
{
    public interface IAuthorizationInfoReader
    {
        Task<bool> CheckAuthority(int chainId, Address mSigAddress, IEnumerable<byte[]> pubKeys);
        Task<Kernel.Proposal> GetProposal(int chainId, Hash proposalHash);
        Task<Authorization> GetAuthorization(int chainId, Address msig);
        bool ValidateAuthorization(Authorization authorization, IEnumerable<byte[]> pubKeys);
    }
}
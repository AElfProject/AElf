using System.Collections.Generic;
using AElf.Common;
using AElf.Kernel;
using Google.Protobuf;
using Authorization = AElf.Kernel.Types.Proposal.Authorization;

namespace AElf.SmartContract.Proposal
{
    public interface IAuthorizationInfo
    {
        bool CheckAuthority(Address mSigAddress, IEnumerable<ByteString> pubKeys);
        bool CheckAuthority(Transaction transaction);
        Kernel.Types.Proposal.Proposal GetProposal(Hash proposalHash);
        Authorization GetAuthorization(Address msig);
    }
}
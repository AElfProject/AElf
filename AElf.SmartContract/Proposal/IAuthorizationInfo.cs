using System.Collections.Generic;
using AElf.Common;
using AElf.Kernel;
using Google.Protobuf;

namespace AElf.SmartContract.Proposal
{
    public interface IAuthorizationInfo
    {
        bool CheckAuthority(Address mSigAddress, IEnumerable<byte[]> pubKeys);
        bool CheckAuthority(Transaction transaction);
        Kernel.Proposal GetProposal(Hash proposalHash);
        Authorization GetAuthorization(Address msig);
    }
}
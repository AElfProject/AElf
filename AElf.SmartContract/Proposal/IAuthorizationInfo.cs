using System.Collections.Generic;
using AElf.Common;
using AElf.Kernel;
using Google.Protobuf;

namespace AElf.SmartContract.Proposal
{
    public interface IAuthorizationInfo
    {
        bool CheckAuthority(Address mSigAddress, IEnumerable<ByteString> pubKeys);
        bool CheckAuthority(Transaction transaction);
    }
}
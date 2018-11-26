using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using Google.Protobuf;

namespace AElf.ChainController.MSig
{
    public interface IAuthorizationInfo
    {
        bool CheckAuthority(Address mSigAddress, IEnumerable<ByteString> pubKeys);
    }
}
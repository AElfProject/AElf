using System.Collections.Generic;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.Token
{
    public interface ITokenContractInitializationDataProvider
    {
        TokenContractInitializationData GetContractInitializationData();
    }

    public class TokenContractInitializationData
    {
        public ByteString NativeTokenInfoData { get; set; }
        public ByteString ResourceTokenListData { get; set; }
        public Dictionary<string, int> ResourceAmount { get; set; }
        public Dictionary<int, Address> RegisteredOtherTokenContractAddresses { get; set; }
        public Address Creator { get; set; }
        public ByteString PrimaryTokenInfoData { get; set; }
        public Dictionary<Address, long> TokenInitialIssueList { get; set; }
    }
}
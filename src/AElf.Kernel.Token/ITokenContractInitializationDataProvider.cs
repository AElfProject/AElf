using System;
using System.Collections.Generic;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.Token
{
    /// <summary>
    /// Add this interface because the initialization logic of Token Contract
    /// are different from Main Chain, Side Chain and test cases.
    /// </summary>
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
        public List<TokenInitialIssue> TokenInitialIssueList { get; set; }
    }

    public class TokenInitialIssue
    {
        public Address Address { get; set; }
        public long Amount { get; set; }
    }
}
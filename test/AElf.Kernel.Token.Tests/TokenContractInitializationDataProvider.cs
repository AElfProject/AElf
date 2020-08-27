using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.Token.Test
{
    internal static class TokenInfoGeneratorHelper
    {
        internal static ByteString GenerateTokenInfoByteStringBySymbol(string tokenSymbol)
        {
            return GenerateTokenInfoBySymbol(tokenSymbol).ToByteString();
        }

        private static TokenInfo GenerateTokenInfoBySymbol(string tokenSymbol)
        {
            return new TokenInfo
            {
                Symbol = tokenSymbol,
                TokenName = tokenSymbol + "name",
                TotalSupply = 1000_000_000,
                Issuer = SampleAddress.AddressList[0],
            };
        }
        
        internal static ByteString GenerateResourceTokenListByteString()
        {
            var resourceToken1 = GenerateTokenInfoBySymbol("CPU");
            var resourceToken2 = GenerateTokenInfoBySymbol("NET");
            return new TokenInfoList
            {
                Value = { resourceToken1, resourceToken2}
            }.ToByteString();
        }
    }
    
    public class TokenContractInitializationDataProviderWithNull : ITokenContractInitializationDataProvider
    {
        public TokenContractInitializationData GetContractInitializationData()
        {
            return null;
        }
    }

    public class TokenContractInitializationDataProviderWithPrimaryToken : ITokenContractInitializationDataProvider
    {
        private readonly int _issueCount;

        public TokenContractInitializationDataProviderWithPrimaryToken(int issueCount)
        {
            _issueCount = issueCount;
        }
        
        public TokenContractInitializationData GetContractInitializationData()
        {
            var tokenContractInitializationData =  new TokenContractInitializationData
            {
                NativeTokenInfoData = TokenInfoGeneratorHelper.GenerateTokenInfoByteStringBySymbol("ALICE"),
                ResourceTokenListData = TokenInfoGeneratorHelper.GenerateResourceTokenListByteString(),
                PrimaryTokenInfoData = TokenInfoGeneratorHelper.GenerateTokenInfoByteStringBySymbol("ELF"),
                Creator = SampleAddress.AddressList[0],
                TokenInitialIssueList = new List<TokenInitialIssue>(),
                ResourceAmount =  new Dictionary<string, int>{{"CPU", 100}},
                RegisteredOtherTokenContractAddresses = new Dictionary<int, Address>()
            };
            tokenContractInitializationData.TokenInitialIssueList.AddRange(Enumerable.Range(0, _issueCount).Select(x => new TokenInitialIssue
            {
                Address = SampleAddress.AddressList[1],
                Amount = 100
            }));
            return tokenContractInitializationData;
        }
    }
    
    public class TokenContractInitializationDataProviderWithoutPrimaryToken : ITokenContractInitializationDataProvider
    {
        public TokenContractInitializationData GetContractInitializationData()
        {
            return new TokenContractInitializationData
            {
                NativeTokenInfoData = TokenInfoGeneratorHelper.GenerateTokenInfoByteStringBySymbol("ALICE"),
                ResourceTokenListData = TokenInfoGeneratorHelper.GenerateResourceTokenListByteString(),
                Creator = SampleAddress.AddressList[0],
                ResourceAmount =  new Dictionary<string, int>{{"CPU", 100}},
                RegisteredOtherTokenContractAddresses = new Dictionary<int, Address>()
            };
        }
    }
}
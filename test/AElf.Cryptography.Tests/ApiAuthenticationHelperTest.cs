using System;
using Xunit;

namespace AElf.Cryptography.Tests
{
    public class ApiAuthenticationHelperTest
    {
        [Fact]
        public void SignAndVerifyTest()
        {
            var chainApiKey = Guid.NewGuid().ToString("N");
            var chainId = Guid.NewGuid().ToString("N");
            var method = "get";
            var timestamp = ApiAuthenticationHelper.GetTimestamp(DateTime.Now);

            var sign = ApiAuthenticationHelper.GetSign(chainApiKey, chainId, method, timestamp);
            var checkResult = ApiAuthenticationHelper.Check(chainApiKey, chainId, method, timestamp, sign, 5);
            
            Assert.True(checkResult);
        }

        [Fact]
        public void TimeoutTest()
        {
            var chainApiKey = Guid.NewGuid().ToString("N");
            var chainId = Guid.NewGuid().ToString("N");
            var method = "post";
            var timestamp = ApiAuthenticationHelper.GetTimestamp(DateTime.Now.AddMinutes(-6));

            var sign = ApiAuthenticationHelper.GetSign(chainApiKey, chainId, method, timestamp);
            var checkResult = ApiAuthenticationHelper.Check(chainApiKey, chainId, method, timestamp, sign, 5);

            Assert.False(checkResult);
        }
    }
}
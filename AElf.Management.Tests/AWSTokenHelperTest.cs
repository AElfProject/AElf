using AElf.Management.Helper;
using Xunit;

namespace AElf.Management.Tests
{
    public class AWSTokenHelperTest
    {
        [Fact(Skip = "require aws access token")]
        public void GetTokenTest()
        {
            var credential = AWSTokenHelper.GetToken();
            Assert.NotNull(credential.Status.Token);
        }
    }
}
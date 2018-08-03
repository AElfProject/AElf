using Xunit;
using AElf.Deployment.Helper;

namespace AElf.Deployment.Tests
{
    public class AWSTokenHelperTest
    {
        [Fact]
        public void GetTokenTest()
        {
            var credential = AWSTokenHelper.GetToken();
            Assert.NotNull(credential.Status.Token);
        }
    }
}
using Xunit;

namespace AElf.Deployment.Tests
{
    public class DeploymentServiceTest
    {
        [Fact]
        public void CreateDeploymentTest()
        {
            
            var service = new DeploymentService();

            var pods = service.GetPods();
            
            service.CreateDeployment();
        }
        
        [Fact]
        public void ScaleDeploymentTest()
        {
            
            var service = new DeploymentService();
            
            service.Scale();
        }
    }
}
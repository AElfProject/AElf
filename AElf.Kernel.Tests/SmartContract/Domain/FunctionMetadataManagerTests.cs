using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Domain
{
    public class FunctionMetadataManagerTests : AElfKernelTestBase
    {
        private readonly FunctionMetadataManager _functionMetadataManager;
        private int _chainId = 1;
        public FunctionMetadataManagerTests()
        {
            _functionMetadataManager = GetRequiredService<FunctionMetadataManager>();
        }

        [Fact]
        public async Task Add_Metadata_Success()
        {
            var metadataName = "TestMetadataAdd";
            
            var existMetadata = await _functionMetadataManager.GetMetadataAsync(_chainId, metadataName);
            existMetadata.ShouldBeNull();

            await _functionMetadataManager.AddMetadataAsync(_chainId, metadataName, new FunctionMetadata());
            
            existMetadata = await _functionMetadataManager.GetMetadataAsync(_chainId, metadataName);
            existMetadata.ShouldNotBeNull();
        }

        [Fact]
        public async Task Remove_Metadata_Success()
        {
            var metadataName = "TestMetadataRemove";
            
            var existMetadata = await _functionMetadataManager.GetMetadataAsync(_chainId, metadataName);
            existMetadata.ShouldBeNull();
            
            await _functionMetadataManager.AddMetadataAsync(_chainId, metadataName, new FunctionMetadata());
            await _functionMetadataManager.RemoveMetadataAsync(_chainId, metadataName);
            
            existMetadata = await _functionMetadataManager.GetMetadataAsync(_chainId, metadataName);
            existMetadata.ShouldBeNull();
        }
        
        [Fact]
        public async Task Remove_Metadata_NotExist()
        {
            var metadataName = "TestMetadataRemove";

            var existMetadata = await _functionMetadataManager.GetMetadataAsync(_chainId, metadataName);
            existMetadata.ShouldBeNull();
            
            await _functionMetadataManager.RemoveMetadataAsync(_chainId, metadataName);
        }

        [Fact]
        public async Task Add_CallGraph_Success()
        {
            var graph = await _functionMetadataManager.GetCallGraphAsync(_chainId);
            graph.ShouldBeNull();

            await _functionMetadataManager.AddCallGraphAsync(_chainId, new SerializedCallGraph());
            
            graph = await _functionMetadataManager.GetCallGraphAsync(_chainId);
            graph.ShouldNotBeNull();
        }
    }
}
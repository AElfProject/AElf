using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Domain
{
    public class FunctionMetadataManagerTests : AElfKernelTestBase
    {
        private readonly FunctionMetadataManager _functionMetadataManager;
        public FunctionMetadataManagerTests()
        {
            _functionMetadataManager = GetRequiredService<FunctionMetadataManager>();
        }

        [Fact]
        public async Task Add_Metadata_Success()
        {
            var metadataName = "TestMetadataAdd";
            
            var existMetadata = await _functionMetadataManager.GetMetadataAsync(metadataName);
            existMetadata.ShouldBeNull();

            await _functionMetadataManager.AddMetadataAsync(metadataName, new FunctionMetadata());
            
            existMetadata = await _functionMetadataManager.GetMetadataAsync(metadataName);
            existMetadata.ShouldNotBeNull();
        }

        [Fact]
        public async Task Remove_Metadata_Success()
        {
            var metadataName = "TestMetadataRemove";
            
            var existMetadata = await _functionMetadataManager.GetMetadataAsync(metadataName);
            existMetadata.ShouldBeNull();
            
            await _functionMetadataManager.AddMetadataAsync(metadataName, new FunctionMetadata());
            await _functionMetadataManager.RemoveMetadataAsync(metadataName);
            
            existMetadata = await _functionMetadataManager.GetMetadataAsync(metadataName);
            existMetadata.ShouldBeNull();
        }
        
        [Fact]
        public async Task Remove_Metadata_NotExist()
        {
            var metadataName = "TestMetadataRemove";

            var existMetadata = await _functionMetadataManager.GetMetadataAsync(metadataName);
            existMetadata.ShouldBeNull();
            
            await _functionMetadataManager.RemoveMetadataAsync(metadataName);
        }

        [Fact]
        public async Task Add_CallGraph_Success()
        {
            var graph = await _functionMetadataManager.GetCallGraphAsync();
            graph.ShouldBeNull();

            await _functionMetadataManager.AddCallGraphAsync(new SerializedCallGraph());
            
            graph = await _functionMetadataManager.GetCallGraphAsync();
            graph.ShouldNotBeNull();
        }
    }
}
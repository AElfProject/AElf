using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.TestBase;
using Grpc.Core;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace AElf.OS.Network
{
    public class GrpcNetworkTestBase : AElfIntegratedTest<GrpcNetworkTestModule>
    {
    }
    
    public class GrpcNetworkDialerTestBase : AElfIntegratedTest<GrpcNetworkDialerTestModule>
    {}

    public class GrpcBasicNetworkTestBase : AElfIntegratedTest<GrpcBasicNetworkTestModule>
    {
    }

    public class GrpcNetworkWithBootNodesTestBase : AElfIntegratedTest<GrpcNetworkWithBootNodesTestModule>
    {
    }

    public class GrpcBackpressureTestBase : AElfIntegratedTest<GrpcBackpressureTestModule>
    {
    }

    public class TestAsyncStreamReader<T> : IAsyncStreamReader<T>
    {
        private IEnumerator<T> _enumerator;
        public TestAsyncStreamReader(IEnumerable<T> data)
        {
            _enumerator = data.GetEnumerator();
        }
        public void Dispose()
        {
            _enumerator.Dispose();
        }
        public Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            return Task.FromResult(_enumerator.MoveNext());
        }
        
        public T Current => _enumerator.Current;
    }
}
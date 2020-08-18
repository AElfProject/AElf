using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.TestBase;
using Grpc.Core;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace AElf.OS.Network.Grpc
{
    public class GrpcNetworkWithChainTestBase : AElfIntegratedTest<GrpcNetworkWithChainTestModule>
    {
    }

    public class GrpcNetworkTestBase : AElfIntegratedTest<GrpcNetworkTestModule>
    {
    }
    
    public class GrpcNetworkWithPeerTestBase : AElfIntegratedTest<GrpcNetworkWithPeerTestModule>
    {
    }
    
    public class GrpcNetworkWithBootNodesTestBase : AElfIntegratedTest<GrpcNetworkWithBootNodesTestModule>
    {
    }
    
    public class GrpcNetworkConnectionWithBootNodesTestBase : AElfIntegratedTest<GrpcNetworkConnectionWithBootNodesTestModule>
    {
    }
    
    public class GrpcNetworkWithChainAndPeerTestBase : AElfIntegratedTest<GrpcNetworkWithChainAndPeerTestModule>
    {
    }

    public class PeerDialerTestBase : AElfIntegratedTest<PeerDialerTestModule>
    {
    }
    
    public class PeerDialerInvalidHandshakeTestBase : AElfIntegratedTest<PeerDialerInvalidHandshakeTestModule>
    {
    }
    
    public class PeerDialerReplyErrorTestBase : AElfIntegratedTest<PeerDialerReplyErrorTestModule>
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
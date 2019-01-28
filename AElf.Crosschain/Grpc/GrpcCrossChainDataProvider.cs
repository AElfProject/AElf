using System.Collections.Generic;
using AElf.Kernel;

namespace AElf.Crosschain.Grpc
{
    public class GrpcCrossChainDataProvider : ICrossChainDataProvider
    {
        public GrpcCrossChainDataProvider()
        {
            // subscribe new cross chain data event here
        }

        public bool GetSideChainBlockInfo(ref SideChainBlockInfo[] sideChainBlockInfo)
        {
            throw new System.NotImplementedException();
        }

        public bool GetParentChainBlockInfo(ref ParentChainBlockInfo[] parentChainBlockInfo)
        {
            throw new System.NotImplementedException();
        }
    }
}
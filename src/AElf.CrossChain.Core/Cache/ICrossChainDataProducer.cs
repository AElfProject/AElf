using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace AElf.CrossChain.Cache
{
    public interface ICrossChainDataProducer
    {
        bool AddNewBlockInfo(IBlockInfo blockInfo);
        ILogger<CrossChainDataProducer> Logger { get; set; }
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Managers;

namespace AElf.Kernel.Consensus
{
    // ReSharper disable UnusedMember.Global
    // ReSharper disable InconsistentNaming
    public class AElfDPoSInfoProvider
    {
        private readonly IMinersManager _minersManager;

        public AElfDPoSInfoProvider(IMinersManager minersManager)
        {
            _minersManager = minersManager;
        }

        public async Task<List<Address>> GetMinersList()
        {
            return (await _minersManager.GetMiners()).Nodes.ToList();
        }
        
        
    }
}
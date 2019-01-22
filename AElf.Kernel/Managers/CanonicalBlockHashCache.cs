using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using AElf.Kernel.EventMessages;
using Easy.MessageHub;
using AElf.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Managers
{
    public class CanonicalBlockHashCache
    {
        private readonly ILightChain _lightChain;
        private int _filling;
        public ILogger<CanonicalBlockHashCache> Logger {get;set;}
        private ulong _currentHeight;

        public ulong CurrentHeight
        {
            get
            {
                if (_currentHeight == default(ulong))
                {
                    RecoverCurrent().Wait();
                }

                return _currentHeight;
            }
        }
        
        private readonly ConcurrentDictionary<ulong, Hash> _blocks = new ConcurrentDictionary<ulong, Hash>();

        public CanonicalBlockHashCache(ILightChain lightChain)
        {
            _lightChain = lightChain;
            Logger = NullLogger<CanonicalBlockHashCache>.Instance;
            MessageHub.Instance.Subscribe<BlockHeader>(
                async h => await OnNewBlockHeader(h));

            MessageHub.Instance.Subscribe<BranchRolledBack>(
                async r => await RecoverCurrent());
        }

        public Hash GetHashByHeight(ulong height)
        {
            if (_blocks.TryGetValue(height, out var hash)) return hash;

            if (_blocks.Count == 0)
            {
                RecoverCurrent().Wait();
            }

            _blocks.TryGetValue(height, out hash);
            return hash;
        }

        public async Task OnNewBlockHeader(BlockHeader header)
        {
            var height = header.Index;
            if (_blocks.Count == 0)
            {
                // If empty, just add
                AddToBlocks(height, header.GetHash());
            }
            else if (_blocks.TryGetValue(height - 1, out var prevHash) && prevHash == header.PreviousBlockHash)
            {
                // Current fork
                AddToBlocks(height, header.GetHash());
                if (height > GlobalConfig.ReferenceBlockValidPeriod)
                {
                    var toRemove = height - GlobalConfig.ReferenceBlockValidPeriod - 1;
                    if (_blocks.TryRemove(toRemove, out _))
                    {
                        Logger.LogTrace($"Removing Canonical Hash of height {toRemove}.");
                    }
                }
            }
            else
            {
                // Switch fork
                //_blocks.Clear();
                AddToBlocks(height, header.GetHash());
            }

            _currentHeight = height;
            await MaybeFillBlocks();
        }

        private void AddToBlocks(ulong height, Hash blockHash)
        {
            Logger.LogTrace($"Adding Canonical Hash {blockHash.ToHex()} of height {height}");
            if (!_blocks.ContainsKey(height))
            {
                _blocks.TryAdd(height, blockHash);
                return;
            }

            _blocks[height] = blockHash;
        }

        private async Task MaybeFillBlocks()
        {
            var height = _currentHeight;
            if (Interlocked.CompareExchange(ref _filling, 1, 0) == 0)
            {
                for (var i = (ulong) 1; i <= Math.Max(GlobalConfig.ReferenceBlockValidPeriod, height); i++)
                {
                    if (height < i)
                    {
                        break;
                    }

                    if (_blocks.ContainsKey(height))
                    {
                        break;
                    }

                    await _lightChain.GetCanonicalHashAsync(height - i);
                }
            }
        }

        private async Task RecoverCurrent()
        {
            _blocks.Clear();
            var curHeight = await _lightChain.GetCurrentBlockHeightAsync();
            var curHeader = await _lightChain.GetHeaderByHeightAsync(curHeight);
            if (curHeader != null)
            {
                await OnNewBlockHeader((BlockHeader) curHeader);    
            }
            // TODO: curHeader should never be null, so maybe exception needs to be thrown
        }
    }
}
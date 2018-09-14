using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;
using AElf.Common.ByteArrayHelpers;
using AElf.Kernel;
using Easy.MessageHub;
using ServiceStack;

namespace AElf.Kernel.Managers
{
    public class CanonicalBlockHashCache
    {
        private ILightChain _lightChain;
        private int _switching;

        public bool SwitchingFork
        {
            get => _switching > 0;
        }

        public ulong CurrentHeight { get; private set; }

        private readonly ConcurrentDictionary<ulong, Hash> _blocks = new ConcurrentDictionary<ulong, Hash>();

        public CanonicalBlockHashCache(ILightChain lightChain)
        {
            _lightChain = lightChain;
            MessageHub.Instance.Subscribe<BlockHeader>(
                async h => await OnNewBlockHeader(h));
        }

        public Hash GetHashByHeight(ulong height)
        {
            _blocks.TryGetValue(height, out var hash);
            return hash;
        }

        public async Task OnNewBlockHeader(BlockHeader header)
        {
            var height = header.Index;
            if (height == 0)
            {
                _blocks.TryAdd(0, header.GetHash());
            }
            else if (_blocks.TryGetValue(height - 1, out var prevHash) && prevHash == header.PreviousBlockHash)
            {
                var added = _blocks.TryAdd(height, header.GetHash());
                if (added && height > Globals.ReferenceBlockValidPeriod)
                {
                    var toRemove = height - Globals.ReferenceBlockValidPeriod - 1;
                    _blocks.TryRemove(toRemove, out var rmd);
                }
            }
            else
            {
                if (Interlocked.CompareExchange(ref _switching, 1, 0) == 0)
                {
                    _blocks.Clear();
                    await RefillBlocks(header.Index);
                    Interlocked.CompareExchange(ref _switching, 0, 1);
                }
            }

            CurrentHeight = height;

        }

        private async Task RefillBlocks(ulong lastBlockHeight)
        {
            if (Interlocked.CompareExchange(ref _switching, 1, 0) == 0)
            {
                for (var i = (ulong) 0; i <= Globals.ReferenceBlockValidPeriod; i++)
                {
                    if (lastBlockHeight < i)
                    {
                        break;
                    }

                    await _lightChain.GetCanonicalHashAsync(lastBlockHeight - i);
                }
            }
        }
    }
}
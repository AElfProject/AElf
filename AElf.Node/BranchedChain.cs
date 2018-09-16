using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Kernel;
using AElf.Node.Protocol;

// ReSharper disable once CheckNamespace
namespace AElf.Node
{
    public class BranchedChain
    {
        public BranchedChain(PendingBlock first, IReadOnlyCollection<PendingBlock> list)
        {
            PendingBlocks.Add(first);

            foreach (var pendingBlock in list)
            {
                PendingBlocks.Add(pendingBlock);
            }
            
            EndHeight = list.Last().Block.Header.Index;
        }

        public BranchedChain(IEnumerable<PendingBlock> list, PendingBlock last)
        {
            foreach (var pendingBlock in list)
            {
                PendingBlocks.Add(pendingBlock);
            }

            PendingBlocks.Add(last);

            EndHeight = last.Block.Header.Index;
        }

        public BranchedChain(IEnumerable<PendingBlock> list1, IReadOnlyCollection<PendingBlock> list2)
        {
            foreach (var pendingBlock in list1)
            {
                PendingBlocks.Add(pendingBlock);
            }

            foreach (var pendingBlock in list2)
            {
                PendingBlocks.Add(pendingBlock);
            }
            
            EndHeight = list2.Last().Block.Header.Index;
        }

        public BranchedChain(PendingBlock first)
        {
            PendingBlocks.Add(first);
            EndHeight = first.Block.Header.Index;
        }

        public List<PendingBlock> GetPendingBlocks()
        {
            return PendingBlocks.OrderBy(pb => pb.Block.Header.Index).ToList();
        }

        public bool CanCheckout(ulong localHeight, Hash blockHash)
        {
            return IsContinuous && EndHeight > localHeight && blockHash != PendingBlocks.First()?.BlockHash;
        }

        public bool IsContinuous
        {
            get
            {
                if (PendingBlocks.Count <= 0)
                {
                    return false;
                }

                Hash preBlockHash = PendingBlocks[0].BlockHash;
                for (var i = 1; i < PendingBlocks.Count; i++)
                {
                    if (PendingBlocks[i].Block.Header.PreviousBlockHash != preBlockHash)
                    {
                        return false;
                    }

                    preBlockHash = PendingBlocks[i].BlockHash;
                }

                return true;
            }
        }

        public ulong EndHeight { get; set; }

        private List<PendingBlock> PendingBlocks { get; set; } = new List<PendingBlock>();

        public Hash PreBlockHash =>
            PendingBlocks.Count <= 0 ? null : PendingBlocks.First().Block.Header.PreviousBlockHash;

        public Hash LastBlockHash => PendingBlocks.Count <= 0 ? null : PendingBlocks.Last().BlockHash;
    }
}
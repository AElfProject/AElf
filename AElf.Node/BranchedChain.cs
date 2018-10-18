using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Common;
using AElf.Node.Protocol;

// ReSharper disable once CheckNamespace
namespace AElf.Node
{
    public class BranchedChain : IComparable<BranchedChain>
    {
        public BranchedChain(PendingBlock first, IReadOnlyCollection<PendingBlock> list)
        {
            PendingBlocks.Add(first);

            foreach (var pendingBlock in list)
            {
                PendingBlocks.Add(pendingBlock);
            }

            PendingBlocks.SortByBlockIndex();
            EndHeight = PendingBlocks.Last().Block.Header.Index;
        }

        public BranchedChain(IEnumerable<PendingBlock> list, PendingBlock last)
        {
            foreach (var pendingBlock in list)
            {
                PendingBlocks.Add(pendingBlock);
            }

            PendingBlocks.Add(last);

            PendingBlocks.SortByBlockIndex();
            EndHeight = PendingBlocks.Last().Block.Header.Index;
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

            PendingBlocks.SortByBlockIndex();
            EndHeight = PendingBlocks.Last().Block.Header.Index;
        }

        public BranchedChain(PendingBlock first)
        {
            PendingBlocks.Add(first);

            PendingBlocks.SortByBlockIndex();
            EndHeight = PendingBlocks.Last().Block.Header.Index;
        }

        public BranchedChain(List<PendingBlock> list)
        {
            PendingBlocks = list;

            PendingBlocks.SortByBlockIndex(); 
            EndHeight = PendingBlocks.Last().Block.Header.Index;
        }

        public List<PendingBlock> GetPendingBlocks()
        {
            return PendingBlocks.OrderBy(pb => pb.Block.Header.Index).ToList();
        }

        public bool CanCheckout(ulong localHeight)
        {
            return IsContinuous && EndHeight > localHeight;
        }

        public bool IsContinuous
        {
            get
            {
                if (PendingBlocks.Count <= 0)
                {
                    return false;
                }

                var preBlockHash = PendingBlocks[0].Block.GetHash();
                for (var i = 1; i < PendingBlocks.Count; i++)
                {
                    if (PendingBlocks[i].Block.Header.PreviousBlockHash != preBlockHash)
                    {
                        return false;
                    }

                    preBlockHash = PendingBlocks[i].Block.GetHash();
                }

                return true;
            }
        }
        public int CompareTo(BranchedChain other) { return EndHeight.CompareTo(other.EndHeight); }

        public ulong EndHeight { get; set; }

        public ulong StartHeight => PendingBlocks.Any() ? PendingBlocks[0].Block.Header.Index : 1;

        private List<PendingBlock> PendingBlocks { get; set; } = new List<PendingBlock>();

        public Hash PreBlockHash =>
            PendingBlocks.Count <= 0 ? null : PendingBlocks.First().Block.Header.PreviousBlockHash;

        public Hash LastBlockHash => PendingBlocks.Count <= 0 ? null : PendingBlocks.Last().Block.GetHash();
    }
}
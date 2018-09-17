using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common.Extensions;
using AElf.Kernel;
using AElf.Node.Protocol;
using NLog;
using NServiceKit.Common;

namespace AElf.Node
{
    public static class PendingBlockExtensions
    {
        private static PendingBlock _targetBlock;
        private static bool _isInitialSync = true;
        public static bool IsConsensusGenerator = false;

        public static bool AddPendingBlock(this List<PendingBlock> pendingBlocks, PendingBlock pendingBlock)
        {
            if (IsConsensusGenerator)
            {
                _isInitialSync = false;
            }

            if (_isInitialSync)
            {
                if (pendingBlocks.IsEmpty())
                {
                    ConsoleWriteLine(nameof(AddPendingBlock),
                        $"Add target block: {pendingBlock.Block.GetHash().ToHex()} - {pendingBlock.Block.Header.Index}");
                    _targetBlock = pendingBlock;
                    pendingBlocks.Add(_targetBlock);
                    return true;
                }

                if (pendingBlock.Block.Header.Index >= _targetBlock?.Block.Header.Index)
                {
                    return false;
                }

                pendingBlocks.Add(pendingBlock);
                ConsoleWriteLine(nameof(AddPendingBlock),
                    $"Added pending block: {pendingBlock.Block.GetHash().ToHex()} - {pendingBlock.Block.Header.Index}");
                
                // Add initial sync blocks finished.
                if (_targetBlock != null &&
                    new Hash(pendingBlock.Block.GetHash()) == _targetBlock.Block.Header.PreviousBlockHash)
                {
                    ConsoleWriteLine(nameof(AddPendingBlock), "Change sync state.");
                    _isInitialSync = false;
                    _targetBlock = null;
                }

                return true;
            }

            if (!_isInitialSync && pendingBlocks.IsEmpty())
            {
                ConsoleWriteLine(nameof(AddPendingBlock),
                    $"Added pending block when list is empty: {pendingBlock.Block.GetHash().ToHex()} - {pendingBlock.Block.Header.Index}");
                
                pendingBlocks.Add(pendingBlock);
                return true;
            }

            if (pendingBlocks.Last().Block.GetHash() != pendingBlock.Block.Header.PreviousBlockHash)
            {
                return false;
            }

            pendingBlocks.Add(pendingBlock);
            ConsoleWriteLine(nameof(AddPendingBlock),
                $"Added pending block: {pendingBlock.Block.GetHash().ToHex()} - {pendingBlock.Block.Header.Index}");

            pendingBlocks.Print();
            return true;
        }

        public static void SortByBlockIndex(this List<PendingBlock> pendingBlocks)
        {
            pendingBlocks.Sort(ComparePendingBlockIndex);
        }

        public static void Print(this IEnumerable<PendingBlock> pendingBlocks)
        {
            ConsoleWriteLine(nameof(Print), "Current PendingBlocks:");
            foreach (var pendingBlock in pendingBlocks)
            {
                Console.WriteLine($"{pendingBlock.Block.GetHash().ToHex()} - {pendingBlock.Block.Header.Index}");
            }
        }

        private static int ComparePendingBlockIndex(PendingBlock block1, PendingBlock block2)
        {
            if (block1 != null)
            {
                if (block2 == null)
                {
                    return 1;
                }

                return Compare(block1, block2);
            }

            if (block2 == null)
            {
                return 0;
            }

            return -1;
        }

        private static int Compare(PendingBlock block1, PendingBlock block2)
        {
            if (block1.Block.Header.Index > block2.Block.Header.Index)
            {
                return 1;
            }

            if (block1.Block.Header.Index < block2.Block.Header.Index)
            {
                return -1;
            }

            return 0;
        }

        private static void ConsoleWriteLine(string prefix, string log, Exception ex = null)
        {
            Console.WriteLine($"[{GetLocalTime():HH:mm:ss} - PendingBlockExtensions]{prefix} - {log}");
            if (ex != null)
            {
                Console.WriteLine(ex);
            }
        }

        private static DateTime GetLocalTime()
        {
            return DateTime.UtcNow.ToLocalTime();
        }
    }
}
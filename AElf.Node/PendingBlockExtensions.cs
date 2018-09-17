using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Common.Extensions;
using AElf.Kernel;
using AElf.Node.Protocol;
using NLog;
using NServiceKit.Common;
using NServiceKit.Text;

namespace AElf.Node
{
    public static class PendingBlockExtensions
    {
        public static bool IsConsensusGenerator = false;

        public static void SortByBlockIndex(this List<PendingBlock> pendingBlocks)
        {
            pendingBlocks.Sort(ComparePendingBlockIndex);
        }

        public static void Print(this List<PendingBlock> pendingBlocks)
        {
            if (pendingBlocks.IsNullOrEmpty())
            {
                ConsoleWriteLine(nameof(Print), "Current PendingBlocks list is empty.");
            }
            else
            {
                ConsoleWriteLine(nameof(Print), "Current PendingBlocks:");
                foreach (var pendingBlock in pendingBlocks)
                {
                    Console.WriteLine($"{pendingBlock.Block.GetHash().ToHex()} - {pendingBlock.Block.Header.Index}");
                }
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
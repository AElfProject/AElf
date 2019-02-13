using System;
using System.Linq;
using System.Numerics;
using System.Text;

namespace AElf.Common
{
    public static class ChainHelpers
    {
        public static int GetRandomChainId()
        {
            Random r = new Random();
            return r.Next(198535, 11316496);
        }

        public static int GetChainId(ulong serialNumber)
        {
            return 198535 + (int) ((uint) serialNumber.GetHashCode() % 11316496) % 11316496;
        }
    }
}
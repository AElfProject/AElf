using AElf.Kernel.Extensions;
using AElf.Kernel.Merkle;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Shouldly;
using Xunit;


namespace AElf.Kernel.Tests
{
    public class MinerTest
    {
        [Fact]
        public void MineTest()
        {
            var block = new Block(new Hash<IBlock>("aelf".CalculateHash()));

            CreateLeaves(new[] { "a", "e", "l", "f" }).ForEach(l => block.GetHeader().AddTransaction(l));
        }

        #region Some methods
        private static List<IHash<ITransaction>> CreateLeaves(IEnumerable<string> buffers)
        {
            return buffers.Select(buffer => new Hash<ITransaction>(buffer.CalculateHash())).Cast<IHash<ITransaction>>().ToList();
        }
        #endregion
    }
}

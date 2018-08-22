using AElf.Cryptography.SSL;
using Xunit;

namespace AElf.Cryptography.Tests.SSL
{
    public class PemWriterTest
    {
        [Fact]
        public void WriterTest()
        {
            Writer writer = new Writer();
            writer.PerformTest();
        }
    }
}
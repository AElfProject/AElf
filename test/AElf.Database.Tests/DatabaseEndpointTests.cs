using Shouldly;
using Xunit;

namespace AElf.Database.Tests
{
    public class DatabaseEndpointTests
    {
        [Fact]
        public void Parse_From_Connection_String_Test()
        {
            var redisConnectionString = "redis://192.168.9.9:6379?db=1";
            var redisEndpoint = DatabaseEndpoint.ParseFromConnectionString(redisConnectionString);
            redisEndpoint.Host.ShouldBe("192.168.9.9");
            redisEndpoint.Port.ShouldBe(6379);
            redisEndpoint.DatabaseNumber.ShouldBe(1);
            redisEndpoint.DatabaseType.ShouldBe(DatabaseType.Redis);

            var ssdbConnectionString = "ssdb://192.168.8.8:8888";
            var ssdbEndpoint = DatabaseEndpoint.ParseFromConnectionString(ssdbConnectionString);
            ssdbEndpoint.Host.ShouldBe("192.168.8.8");
            ssdbEndpoint.Port.ShouldBe(8888);
            ssdbEndpoint.DatabaseType.ShouldBe(DatabaseType.SSDB);
        }
    }
}
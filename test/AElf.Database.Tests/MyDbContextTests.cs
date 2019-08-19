using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AElf.Database.Tests
{
    public class MyDbContextTests: KeyValueDbContextTestBase<MyContext>
    {
        [Fact]
        public void CheckDbType_Test()
        {
            var type = this._context.Database.GetType().GetGenericTypeDefinition();
            typeof(InMemoryDatabase<>).IsAssignableFrom(type).ShouldBe(true);

            var connection = GetRequiredService<KeyValueDatabaseOptions<MyContext>>().ConnectionString;
            
            connection.ShouldBe( "127.0.0.1");
        }
    }
}
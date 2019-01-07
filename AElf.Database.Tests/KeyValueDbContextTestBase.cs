using System;
using Xunit;

namespace AElf.Database.Tests
{
    public abstract class KeyValueDbContextTestBase<TKeyValueDbContext> : DatabaseTestBase
        where TKeyValueDbContext : KeyValueDbContext<TKeyValueDbContext>
    {
        private TKeyValueDbContext _context;

        private IKeyValueDatabase<TKeyValueDbContext> _database;

        protected KeyValueDbContextTestBase()
        {
            // ReSharper disable once VirtualMemberCallInConstructor
            _context = GetRequiredService<TKeyValueDbContext>();
            _database = _context.Database;
        }


        [Fact]
        public void IsConnectedTest()
        {
            var result = _database.IsConnected();
            Assert.True(result);
        }

        [Fact]
        public void SetTest()
        {
            var key = "settest";
            var value = Guid.NewGuid().ToString();

            _database.SetAsync(key, Helper.StringToBytes(value));
        }

        [Fact]
        public void GetTest()
        {
            var key = "gettest";
            var value = Guid.NewGuid().ToString();

            _database.SetAsync(key, Helper.StringToBytes(value));
            var getResult = _database.GetAsync(key);

            Assert.Equal(value, Helper.BytesToString(getResult.Result));
        }
    }
    
    public class MyDbContextTestBase: KeyValueDbContextTestBase<MyContext>
    {
    }
    
    public class InMemoryTestBase: KeyValueDbContextTestBase<InMemoryDbContext>
    {
    }
}
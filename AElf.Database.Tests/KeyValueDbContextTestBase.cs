using System;
using System.Threading.Tasks;
using Xunit;

namespace AElf.Database.Tests
{
    public abstract class KeyValueDbContextTestBase<TKeyValueDbContext> : DatabaseTestBase
        where TKeyValueDbContext : KeyValueDbContext<TKeyValueDbContext>
    {
        protected TKeyValueDbContext _context;

        protected IKeyValueDatabase<TKeyValueDbContext> _database;

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
        public async Task SetTest()
        {
            var key = "settest";
            var value = Guid.NewGuid().ToString();

            await _database.SetAsync(key, Helper.StringToBytes(value));
        }

        [Fact]
        public async Task GetTest()
        {
            var key = "gettest";
            var value = Guid.NewGuid().ToString();

            await _database.SetAsync(key, Helper.StringToBytes(value));
            var getResult = await _database.GetAsync(key);

            Assert.Equal(value, Helper.BytesToString(getResult));
        }
    }
}
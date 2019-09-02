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
        public void IsConnected_Test()
        {
            var result = _database.IsConnected();
            Assert.True(result);
        }

        [Fact]
        public async Task Set_Test()
        {
            var key = "settest";
            var value = Guid.NewGuid().ToString();

            await _database.SetAsync(key, Helper.StringToBytes(value));
        }

        [Fact]
        public async Task Get_Test()
        {
            var key = "gettest";
            var value = Guid.NewGuid().ToString();

            await _database.SetAsync(key, Helper.StringToBytes(value));
            var getResult = await _database.GetAsync(key);

            Assert.Equal(value, Helper.BytesToString(getResult));
        }

        [Fact]
        public void Get_Exception_Test()
        {
            var key = string.Empty;
            Assert.Throws<ArgumentException>(() => { _database.GetAsync(key); });
        }

        [Fact]
        public void Set_Exception_Test()
        {
            var key = string.Empty;
            var value = Guid.NewGuid().ToString();
            Assert.Throws<ArgumentException>(() => { _database.SetAsync(key, Helper.StringToBytes(value)); });
        }

        [Fact]
        public void Remove_Exception_Test()
        {
            var key = string.Empty;
            Assert.Throws<ArgumentException>(() => { _database.RemoveAsync(key); });
        }
    }
}
using System;
using System.Collections.Generic;
using System.Text;
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
        public async Task Remove_Test()
        {
            var key = "removetest";
            var value = Guid.NewGuid().ToString();

            await _database.SetAsync(key, Encoding.UTF8.GetBytes(value));
            var exists = await _database.IsExistsAsync(key);
            Assert.True(exists);

            await _database.RemoveAsync(key);

            exists = await _database.IsExistsAsync(key);
            Assert.False(exists);
        }

        [Fact]
        public async Task Multiple_Test()
        {
            var key1 = "setalltest1";
            var value1 = Guid.NewGuid().ToString();
            var key2 = "setalltest2";
            var value2 = Guid.NewGuid().ToString();
            var key3 = "setalltest3";

            await _database.SetAllAsync(new Dictionary<string, byte[]>
            {
                {key1,Encoding.UTF8.GetBytes(value1)},
                {key2,Encoding.UTF8.GetBytes(value2)}
            });

            var getResult = await _database.GetAllAsync(new List<string> {key1, key2, key3});
            Assert.Equal(value1,Encoding.UTF8.GetString(getResult[0]));
            Assert.Equal(value2,Encoding.UTF8.GetString(getResult[1]));
            
            await _database.RemoveAllAsync(new List<string> {key1, key2, key3});
            
            var exists = await _database.IsExistsAsync(key1);
            Assert.False(exists);
            
            exists = await _database.IsExistsAsync(key2);
            Assert.False(exists);
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
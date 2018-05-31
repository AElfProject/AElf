using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Database.SsdbClient;
using Xunit;

namespace AElf.Database.Tests
{
    public class SsdbClientTest:IDisposable
    {
        private static readonly string _host = "127.0.0.1";
        private static readonly int _port = 8888;
        private Client _client;

        public SsdbClientTest()
        {
            _client =new Client(_host,_port);
            _client.Connect();
            FlushDatabase();
        }
        
        public void Dispose()
        {
            FlushDatabase();
            _client.Close();
        }

        public void FlushDatabase()
        {
            _client.FlushDB(SsdbType.None);
        }

        [Fact]
        public void KeyValueSetTest()
        {
            var key = Guid.NewGuid().ToString();
            var value = Guid.NewGuid().ToString();
            _client.Set(key,value);
        }
                
        [Fact]
        public void KeyValueExistsTest()
        {
            var key = Guid.NewGuid().ToString();
            var value = Guid.NewGuid().ToString();
            var existsResult = _client.Exists(key);
            Assert.False(existsResult);
            _client.Set(key,value);
            existsResult = _client.Exists(key);
            Assert.True(existsResult);
        }
        
        [Fact]
        public void KeyValueGetTest()
        {
            var key = Guid.NewGuid().ToString();
            var value = Guid.NewGuid().ToString();
            _client.Set(key,value);
            string dbValue;
            var executeResult = _client.Get(key,out dbValue);
            Assert.True(executeResult);
            Assert.Equal(value,dbValue);
        }
        
        [Fact]
        public void KeyValueDelTest()
        {
            var key = Guid.NewGuid().ToString();
            var value = Guid.NewGuid().ToString();
            _client.Set(key,value);
            _client.Del(key);
            var existsResult = _client.Exists(key);
            Assert.False(existsResult);
        }
        
        [Fact]
        public void KeyValueEmptyKeyTest()
        {
            var key = string.Empty;
            var value = Guid.NewGuid().ToString();
            Assert.Throws<ArgumentException>(() => _client.Set(key, value));

            var keyByte = Helper.StringToBytes(key);
            Assert.Throws<ArgumentException>(() => _client.Set(keyByte,Helper.StringToBytes(value)));
        }
        
        // Todo rewrite
        [Fact]
        public void KeyValueScanTest()
        {
            var key1 = "a";
            var value1 = Guid.NewGuid().ToString();
            var key2 = "b";
            var value2 = Guid.NewGuid().ToString();
            var key3 = "ab";
            var value3 = Guid.NewGuid().ToString();
            var key4 = "c";
            var value4 = Guid.NewGuid().ToString();
            var key5 = "dd";
            var value5 = Guid.NewGuid().ToString();

            using (var client = new Client(_host, _port))
            {
                client.Connect();
                
                client.Set(key1,value1);
                client.Set(key2,value2);
                client.Set(key3,value3);
                client.Set(key4,value4);
                client.Set(key5,value5);

                var result = client.Scan(key1, key4, 2);
                Assert.True(result.Length==2);
                Assert.True(result.First().Key == key3);
                
                result = client.RScan(key4, key1, 2);
                Assert.True(result.Length==2);
                Assert.True(result.First().Key == key2);

                client.Del(key1);
                client.Del(key2);
                client.Del(key3);
                client.Del(key4);
                client.Del(key5);
            }
        }

        [Fact]
        public void HashTest()
        {
            var name = "unittesthash";
            var key = Guid.NewGuid().ToString();
            var value = Guid.NewGuid().ToString();
            
            using (var client = new Client(_host, _port))
            {
                client.Connect();
                bool result;
                result = client.HExists(name, key);
                Assert.False(result);
                
                var sizeResultBefore =client.HSize(name);
                client.HSet(name,key,value);
                result = client.HExists(name, key);
                Assert.True(result);
                
                var sizeResultAfter =client.HSize(name);
                Assert.True(sizeResultBefore+1==sizeResultAfter);
                
                byte[] getResult;
                result = client.HGet(name, key, out getResult);
                Assert.True(result);
                Assert.Equal(Helper.BytesToString(getResult),value);
                
                client.HDel(name,key);
                result = client.HExists(name, key);
                Assert.False(result);
            }
        }
        
        [Fact]
        public void HashMultiTest()
        {
            var name = "unittesthashmulti";
            var key1 = Guid.NewGuid().ToString();
            var value1 = Guid.NewGuid().ToString();
            var key2 = Guid.NewGuid().ToString();
            var value2 = Guid.NewGuid().ToString();
            
            using (var client = new Client(_host, _port))
            {
                client.Connect();
                
                var kvs = new KeyValuePair<string, string>[2];
                kvs[0]=new KeyValuePair<string, string>(key1,value1);
                kvs[1]=new KeyValuePair<string, string>(key2,value2);
                client.MultiHSet(name, kvs);
                
                var getResult =client.MultiHGet(name,new string[]{key1,key2});
                Assert.True(getResult.Length == 2);
                Assert.True(getResult[0].Key==key1 && Helper.BytesToString(getResult[0].Value)==value1);
                Assert.True(getResult[1].Key==key2 && Helper.BytesToString(getResult[1].Value)==value2);
                
                client.MultiHDel(name,new string[]{key1,key2});
                
                getResult =client.MultiHGet(name,new string[]{key1,key2});
                Assert.True(getResult.Length == 0);
            }
        }
        
        [Fact]
        public void HashScanTest()
        {
            var name = "unittesthashscan";
            var key1 = "a";
            var value1 = Guid.NewGuid().ToString();
            var key2 = "b";
            var value2 = Guid.NewGuid().ToString();
            var key3 = "ab";
            var value3 = Guid.NewGuid().ToString();
            var key4 = "d";
            var value4 = Guid.NewGuid().ToString();
            
            using (var client = new Client(_host, _port))
            {
                client.Connect();
                
                var kvs = new KeyValuePair<string, string>[4];
                kvs[0]=new KeyValuePair<string, string>(key1,value1);
                kvs[1]=new KeyValuePair<string, string>(key2,value2);
                kvs[2]=new KeyValuePair<string, string>(key3,value3);
                kvs[3]=new KeyValuePair<string, string>(key4,value4);
                client.MultiHSet(name, kvs);
                
                var scanResult = client.HScan(name, key1,key2, 1);
                Assert.True(scanResult.Length==1);
                Assert.True(scanResult[0].Key == key3 && Helper.BytesToString(scanResult[0].Value) == value3);
                
                var rscanResult = client.HRScan(name, key2,key1, 1);
                Assert.True(rscanResult.Length==1);
                Assert.True(rscanResult[0].Key == key3 && Helper.BytesToString(rscanResult[0].Value) == value3);
                
                client.MultiHDel(name,new string[]{key1,key2,key3,key4});
            }
        }

        [Fact]
        public void ZSetTest()
        {
            var name = "unittestzset";
            var key = Guid.NewGuid().ToString();
            var score = 1;

            using (var client = new Client(_host, _port))
            {
                client.Connect();
                
                bool result;
                result =client.ZExists(name, key);
                Assert.False(result);

                var sizeResultBefore =client.ZSize(name);
                Assert.True(sizeResultBefore==0);
                
                client.ZSet(name,key,score);
                result =client.ZExists(name, key);
                Assert.True(result);
                
                var sizeResultAfter =client.ZSize(name);
                Assert.True(sizeResultAfter==1);

                long scoreResult;
                client.ZGet(name, key, out scoreResult);
                Assert.Equal(score,scoreResult);
                var incrResult = client.ZIncr(name, key, 2);
                Assert.Equal(score + 2, incrResult);

                client.ZDel(name,key);
                result = client.ZExists(name, key);
                Assert.False(result);
            }
        }
        
        [Fact]
        public void ZSetMultiTest()
        {
            var name = "unittestzsetmulti";
            var key1 = Guid.NewGuid().ToString();
            var score1 = 1;
            var key2 = Guid.NewGuid().ToString();
            var score2 = 2;
            
            using (var client = new Client(_host, _port))
            {
                client.Connect();
                
                var kvs = new KeyValuePair<string, long>[2];
                kvs[0]=new KeyValuePair<string, long>(key1,score1);
                kvs[1]=new KeyValuePair<string, long>(key2,score2);
                client.MultiZSet(name, kvs);
                
                var getResult =client.MultiZGet(name,new string[]{key1,key2});
                Assert.True(getResult.Length == 2);
                Assert.True(getResult[0].Key==key1 && getResult[0].Value==score1);
                Assert.True(getResult[1].Key==key2 && getResult[1].Value==score2);
                
                client.MultiZDel(name,new string[]{key1,key2});
                
                getResult =client.MultiZGet(name,new string[]{key1,key2});
                Assert.True(getResult.Length == 0);
            }
        }
        
        [Fact]
        public void ZSetScanAndRangeTest()
        {
            var name = "unittestzsetscan";
            var key1 = "a";
            var value1 = 1;
            var key2 = "b";
            var value2 = 3;
            var key3 = "ab";
            var value3 = 2;
            var key4 = "d";
            var value4 = 4;
            
            using (var client = new Client(_host, _port))
            {
                client.Connect();
                
                var kvs = new KeyValuePair<string, long>[4];
                kvs[0]=new KeyValuePair<string, long>(key1,value1);
                kvs[1]=new KeyValuePair<string, long>(key2,value2);
                kvs[2]=new KeyValuePair<string, long>(key3,value3);
                kvs[3]=new KeyValuePair<string, long>(key4,value4);
                client.MultiZSet(name, kvs);

                var scanResult = client.ZScan(name, key3,2,4, 1);
                Assert.True(scanResult.Length==1);
                Assert.True(scanResult[0].Key == key2 && scanResult[0].Value == value2);
                
                var rscanResult = client.ZRScan(name, key4,4,2, 1);
                Assert.True(rscanResult.Length==1);
                Assert.True(rscanResult[0].Key == key2 && rscanResult[0].Value == value2);

                var rangeResult = client.ZRange(name, 2, 1);
                Assert.True(rangeResult.Length==1);
                Assert.True(rangeResult[0].Key == key2 && rangeResult[0].Value == value2);
                
                var rrangeResult = client.ZRRange(name, 2, 1);
                Assert.True(rrangeResult.Length==1);
                Assert.True(rrangeResult[0].Key == key3 && rrangeResult[0].Value == value3);

                client.MultiZDel(name,new string[]{key1,key2,key3,key4});
            }
        }

    }
}
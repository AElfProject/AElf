using System;
using Google.Protobuf;
using Xunit;

namespace AElf.Kernel.Tests.SmartContractExecuting
{
    public class ParamType
    {
        [Fact]
        public void StringType()
        {
            var data = "str";
            var param = new Param {StrVal = data};
            
            Assert.True(param.DataCase == Param.DataOneofCase.StrVal);
            Assert.Equal(param.Value().GetType(), data.GetType());
            Assert.Equal(param.Value(), data);

            var bytes = param.ToByteArray();
            
        }
        
        [Fact]
        public void IntType()
        {
            var data = 1;
            var param = new Param {IntVal = data};
            Assert.True(param.DataCase == Param.DataOneofCase.IntVal);
            Assert.Equal(param.Value().GetType(), data.GetType());
            Assert.Equal(param.Value(), data);
        }
        
        
        [Fact]
        public void DoubleType()
        {
            var data = 1.1;
            var param = new Param {DVal = data};
            Assert.True(param.DataCase == Param.DataOneofCase.DVal);
            Assert.Equal(param.Value().GetType(), data.GetType());
            Assert.Equal(param.Value(), data);
        }
        
        [Fact]
        public void UlongType()
        {
            var data = (ulong) 1;
            var param = new Param {UlongVal = data};
            Assert.True(param.DataCase == Param.DataOneofCase.UlongVal);
            Assert.Equal(param.Value().GetType(), data.GetType());
            Assert.Equal(param.Value(), data);
        }
        
        [Fact]
        public void HashType()
        {
            var data = Hash.Generate();
            var param = new Param {HashVal = data};
            Assert.True(param.DataCase == Param.DataOneofCase.HashVal);
            Assert.Equal(param.Value().GetType(), data.GetType());
            Assert.Equal(param.Value(), data);
        }

        [Fact]
        public void Print()
        {
            System.Diagnostics.Debug.WriteLine(ByteString.CopyFromUtf8("AElf").ToBase64());
            System.Diagnostics.Debug.WriteLine(Hash.Zero.Value.ToBase64());
        }
    }
}
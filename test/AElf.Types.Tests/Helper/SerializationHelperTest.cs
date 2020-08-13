using System;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Types.Tests.Helper
{
    public class SerializationHelperTest
    {
        [Fact]
        public void Serialize_And_Deserialize_Test()
        {
            //int
            {
                int originTarget = -1;
                var deserializeTarget = GetDeserializeValue<int>(originTarget);
                deserializeTarget.ShouldBe(originTarget);
            }
            
            //uint
            {
                uint originTarget = uint.MaxValue;
                var deserializeTarget = GetDeserializeValue<uint>(originTarget);
                deserializeTarget.ShouldBe(originTarget);
            }
            
            //bool
            {
                var deserializeTarget = GetDeserializeValue<bool>(true);
                deserializeTarget.ShouldBe(true);
            }
            
            //long
            {
                long originTarget = -1;
                var deserializeTarget = GetDeserializeValue<long>(originTarget);
                deserializeTarget.ShouldBe(originTarget);
            }
            
            //ulong
            {
                ulong originTarget = 111_000_111_000;
                var deserializeTarget = GetDeserializeValue<ulong>(originTarget);
                deserializeTarget.ShouldBe(originTarget);
            }
            
            //enum
            {
                SerializeSupportType originTarget = SerializeSupportType.Long;
                var deserializeTarget = GetDeserializeValue<SerializeSupportType>(originTarget);
                deserializeTarget.ShouldBe(originTarget);
            }
            
            //string
            {
                string originTarget = "test";
                var deserializeTarget = GetDeserializeValue<string>(originTarget);
                deserializeTarget.ShouldBe(originTarget);
            }
            
            var iMessageObject = new LogEvent
            {
                Name = "Test"
            };
            
            //byte[]
            {
                var originTarget = iMessageObject.ToByteString().ToByteArray();
                var deserializeTarget = GetDeserializeValue<byte[]>(originTarget);
                deserializeTarget.Length.ShouldBe(originTarget.Length);
                for(var i = 0; i < deserializeTarget.Length; i ++)
                    deserializeTarget[i].ShouldBe(originTarget[i]);
            }
            
            //IMessage
            {
                var originTarget = iMessageObject;
                var deserializeTarget = GetDeserializeValue<LogEvent>(originTarget);
                deserializeTarget.ShouldBe(originTarget);
            }
            
            // other type
            {
                var originTarget = new SerializationHelperTest();
                Should.Throw<InvalidOperationException>(() => { SerializationHelper.Serialize(originTarget);});
                var dataByte = new byte[0];
                Should.Throw<InvalidOperationException>(() => { SerializationHelper.Deserialize<SerializationHelperTest>(dataByte);});
            }
            
        }

        private T GetDeserializeValue<T>(object ob)
        {
            var serializeTarget = SerializationHelper.Serialize(ob);
            return SerializationHelper.Deserialize<T>(serializeTarget);
        }

        private enum SerializeSupportType
        {
            Int,
            Long
        }
    }
}
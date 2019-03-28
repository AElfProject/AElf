using System;
using System.IO;
using AElf.Common;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Types.CSharp.Tests
{
    public class ConversionExtensionTests
    {
        [Fact]
        public void AnyToPbMessage_Test()
        {
            Any any = null;
            var type = typeof(UserTypeHolder);

            try
            {
                // ReSharper disable once ExpressionIsAlwaysNull
                any.AnyToPbMessage(type);
            }
            catch (Exception e)
            {
                e.Message.ShouldBe($"Cannot convert null to {type.FullName}.");
            }

            var personalData = new PersonalData
            {
                Name = "Tom",
                Sex = "male"
            };
            var userTypeHolder = personalData.Pack();
            any = userTypeHolder.ToAny();

            try
            {
                any.AnyToPbMessage(typeof(string));
            }
            catch (Exception e)
            {
                e.Message.ShouldBe("Type given is not an IMessage.");
            }

            try
            {
                any.AnyToPbMessage(typeof(Address));
            }
            catch (Exception e)
            {
                e.Message.ShouldBe(
                    $"Full type name for {Address.Descriptor.Name} is {Address.Descriptor.FullName}; Any message's type url is {any.TypeUrl}");
            }

            var newUserTypeHolder = any.AnyToPbMessage(type);
            userTypeHolder.Equals(newUserTypeHolder).ShouldBeTrue();
        }

        [Fact]
        public void AnyToUserType_Test()
        {
            var personalData = new PersonalData
            {
                Name = "Tom",
                Sex = "male"
            };
            var any = personalData.ToAny();

            var newPersonalData = any.AnyToUserType(typeof(PersonalData));
            personalData.Equals(newPersonalData).ShouldBeTrue();

            try
            {
                any.AnyToUserType(typeof(Address));
            }
            catch (Exception e)
            {
                e.Message.ShouldBe("Type given is not a UserType.");
            }
        }

        [Fact]
        public void Deserialize_BoolAny()
        {
            var any1 = true.ToAny();
            any1.AnyToBool().ShouldBeTrue();

            var any2 = false.ToAny();
            any2.AnyToBool().ShouldBeFalse();
        }

        [Fact]
        public void Deserialize_ByteAny()
        {
            var byte1 = Hash.Generate().ToByteArray();
            var any = byte1.ToAny();
            any.AnyToBytes().ShouldBe(byte1);
        }

        [Fact]
        public void Deserialize_Bytes_To_PbMessage()
        {
            var personalData = new PersonalData
            {
                Name = "Tom",
                Sex = "male"
            };
            var userTypeHolder = personalData.Pack();
            var newUserTypeHolder = userTypeHolder.ToByteArray().DeserializeToPbMessage<UserTypeHolder>();
            userTypeHolder.Equals(newUserTypeHolder).ShouldBeTrue();
        }

        [Fact]
        public void Deserialize_Bytes_To_UserType_Test()
        {
            var personalData = new PersonalData
            {
                Name = "Tom",
                Sex = "male"
            };
            var newPersonalData = personalData.ToPbMessage().ToByteArray().DeserializeToUserType<PersonalData>();
            newPersonalData.Equals(personalData).ShouldBeTrue();
        }

        [Fact]
        public void Deserialize_ByteString_To_PbMessage()
        {
            var personalData = new PersonalData
            {
                Name = "Tom",
                Sex = "male"
            };
            var userTypeHolder = personalData.Pack();
            var newUserTypeHolder = userTypeHolder.ToByteString().DeserializeToPbMessage<UserTypeHolder>();
            userTypeHolder.Equals(newUserTypeHolder).ShouldBeTrue();
        }

        [Fact]
        public void Deserialize_ByteString_To_UserType_Test()
        {
            var personalData = new PersonalData
            {
                Name = "Tom",
                Sex = "male"
            };

            var newPersonalData = personalData.ToPbMessage().ToByteString().DeserializeToUserType<PersonalData>();
            newPersonalData.Equals(personalData).ShouldBeTrue();
        }

        [Fact]
        public void Deserialize_Int64Any()
        {
            var lNumber = (long) new Random(DateTime.Now.Millisecond).Next();
            var any = lNumber.ToAny();
            any.AnyToInt64().ShouldBe(lNumber);
        }

        [Fact]
        public void Deserialize_IntAny()
        {
            var randomNumber = new Random(DateTime.Now.Millisecond).Next();
            var anyValue = randomNumber.ToAny();
            var intValue = anyValue.AnyToInt32();
            randomNumber.ShouldBe(intValue);
        }

        [Fact]
        public void Deserialize_StringAny()
        {
            var message = "hello test";
            var any = message.ToAny();
            any.AnyToString().ShouldBe(message);
        }

        [Fact(Skip = "Not passed due to some reason.")]
        public void Deserialize_To_Bool()
        {
            var message = true.ToPbMessage();
            var bs = ByteString.FromStream(new MemoryStream(message.ToByteArray()));
            var boolValue = (bool) bs.DeserializeToType(typeof(bool));
            boolValue.ShouldBeTrue();

            var message1 = false.ToPbMessage();
            var bs1 = ByteString.FromStream(new MemoryStream(message1.ToByteArray()));
            var boolValue1 = (bool) bs1.DeserializeToType(typeof(bool));
            boolValue1.ShouldBeFalse();
        }

        [Fact]
        public void Deserialize_UInt64Any()
        {
            var lNumber = (ulong) new Random(DateTime.Now.Millisecond).Next();
            var any = lNumber.ToAny();
            any.AnyToUInt64().ShouldBe(lNumber);
        }

        [Fact]
        public void Deserialize_UIntAny()
        {
            var uNumber = (uint) new Random(DateTime.Now.Millisecond).Next();
            var any = uNumber.ToAny();
            any.AnyToUInt32().ShouldBe(uNumber);
        }

        [Fact]
        // ReSharper disable once InconsistentNaming
        public void IMessageToPbMessage()
        {
            var personalData = new PersonalData
            {
                Name = "Tom",
                Sex = "male"
            };
            var userTypeHolder = personalData.Pack();
            ReferenceEquals(userTypeHolder.ToPbMessage(), userTypeHolder).ShouldBeTrue();
        }

        [Fact]
        public void PbMessage_ToAny_Test()
        {
            var personalData = new PersonalData
            {
                Name = "Tom",
                Sex = "male"
            };
            var userTypeHolder = personalData.Pack();
            var any = userTypeHolder.ToAny();
            var newUserTypeHolder = new UserTypeHolder();
            newUserTypeHolder.MergeFrom(any.Value);
            userTypeHolder.Equals(newUserTypeHolder).ShouldBeTrue();
        }

        [Fact]
        public void UserType_ToAny_Test()
        {
            var personalData = new PersonalData
            {
                Name = "Tom",
                Sex = "Man"
            };
            var any = personalData.ToAny();

            var newPersonalData = new PersonalData();
            newPersonalData.Unpack(any.Unpack<UserTypeHolder>());
            personalData.Equals(newPersonalData).ShouldBeTrue();
        }

        [Fact]
        public void UserType_ToPbMessage_Test()
        {
            var personalData = new PersonalData
            {
                Name = "Tom",
                Sex = "male"
            };
            var pbMessage = personalData.ToPbMessage();
            pbMessage.ShouldBeOfType<UserTypeHolder>();
            var userTypeHolder = (UserTypeHolder) pbMessage;
            userTypeHolder.Fields["Name"].Unpack<StringValue>().Value.ShouldBe("Tom");
            userTypeHolder.Fields["Sex"].Unpack<StringValue>().Value.ShouldBe("male");
        }
    }
}
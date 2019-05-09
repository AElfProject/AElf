using System;
using System.IO;
using Google.Protobuf.WellKnownTypes;
using Org.BouncyCastle.Asn1.Cms;

namespace AElf.Runtime.CSharp.Tests.BadContract
{
    public class BadContract : BadContractContainer.BadContractBase
    {
        public override Empty UpdateDoubleState(DoubleInput input)
        {
            State.Double.Value = input.DoubleValue;

            return new Empty();
        }

        public override Empty UpdateFloatState(FloatInput input)
        {
            State.Float.Value = input.FloatValue;
            
            return new Empty();
        }

        public override RandomOutput UpdateStateWithRandom(Empty input)
        {
            var random = new Random().Next();

            State.CurrentRandom.Value = random;
            
            return new RandomOutput()
            {
                RandomValue = random
            };
        }

        public override DateTimeOutput UpdateStateWithCurrentTime(Empty input)
        {
            var current = DateTime.Now;

            State.CurrentTime.Value = current;

            State.CurrentTimeUtc.Value = DateTime.UtcNow;

            State.CurrentTimeToday.Value = DateTime.Today;

            return new DateTimeOutput()
            {
                DateTimeValue = Timestamp.FromDateTime(current)
            };
        }

        public override Empty WriteFileToNode(FileInfoInput input)
        {
            using (var writer = new StreamWriter(input.FilePath))
            {
                writer.Write(input.FileContent);
            }
            
            return new Empty();
        }
    }
}

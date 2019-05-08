using System;
using System.IO;
using Google.Protobuf.WellKnownTypes;

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
            var current = DateTime.UtcNow;

            State.CurrentTime.Value = current;
            
            return new DateTimeOutput()
            {
                CurrentDateTime = Timestamp.FromDateTime(current)
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

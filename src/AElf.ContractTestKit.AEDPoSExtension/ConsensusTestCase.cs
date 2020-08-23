using System;
using System.ComponentModel;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace AElf.ContractTestKit.AEDPoSExtension
{
    [Serializable]
    public class ConsensusTestCase : XunitTestCase
    {
        private bool _isSideChain;

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the de-serializer", true)]
        public ConsensusTestCase()
        {
        }

        public ConsensusTestCase(
            IMessageSink diagnosticMessageSink,
            TestMethodDisplay testMethodDisplay,
            ITestMethod testMethod,
            bool isSideChain)
            : base(diagnosticMessageSink, testMethodDisplay, TestMethodDisplayOptions.All, testMethod)
        {
            _isSideChain = isSideChain;
        }

        public override void Serialize(IXunitSerializationInfo data)
        {
            base.Serialize(data);
            data.AddValue("IsSideChain", _isSideChain);
        }

        public override void Deserialize(IXunitSerializationInfo data)
        {
            base.Deserialize(data);
            _isSideChain = data.GetValue<bool>("IsSideChain");
        }
    }
}
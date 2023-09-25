using System;

namespace AElf.Cryptography.Core
{

    public interface IECCurve : IDisposable
    {
        IECPoint MultiplyScalar(IECPoint point, IECScalar scalar);
        IECPoint GetPoint(IECScalar scalar);
        byte[] SerializePoint(IECPoint point, bool compressed);
        IECPoint Add(IECPoint point1, IECPoint point2);
        IECPoint Sub(IECPoint point1, IECPoint point2);
        IECPoint DeserializePoint(byte[] input);
        IECScalar DeserializeScalar(byte[] input);
        byte[] GetNonce(IECScalar privateKey, byte[] hash);
    }
}
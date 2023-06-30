using System;

namespace AElf.Cryptography.Core;

public class Secp256k1Scalar:IECScalar
{
    private readonly byte[] _nativeRep;

    private Secp256k1Scalar(byte[] nativeRep)
    {
        _nativeRep = nativeRep;
    }

    public static Secp256k1Scalar FromNative(byte[]nativeRep)
    {
        return new Secp256k1Scalar(nativeRep);
    }
    public byte[] Representation
    {
        get
        {
            var output = new byte[_nativeRep.Length];
            Buffer.BlockCopy(_nativeRep, 0, output, 0, _nativeRep.Length);
            return output;
        }
    }
    
}
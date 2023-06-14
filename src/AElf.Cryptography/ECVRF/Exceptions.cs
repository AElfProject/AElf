using System;

namespace AElf.Cryptography.ECVRF;

public class InvalidSerializedPublicKeyException : Exception
{
}

public class FailedToHashToCurveException : Exception
{
}

public class InvalidProofLengthException : Exception
{
}

public class InvalidScalarException : Exception
{
}

public class FailedToMultiplyScalarException : Exception
{
}

public class FailedToNegatePublicKeyException : Exception
{
}

public class FailedToCombinePublicKeysException : Exception
{
}

public class InvalidProofException : Exception
{
}
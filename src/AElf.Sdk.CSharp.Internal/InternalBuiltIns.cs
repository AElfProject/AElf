// using System;
// using AElf.Sdk.CSharp.Spec;
//
// namespace AElf.Sdk.CSharp.Internal;
//
// internal class InternalBuiltIns : IBuiltIns
// {
//     public bool Ed25519Verify(byte[] signature, byte[] message, byte[] publicKey)
//     {
//         try
//         {
//             var instance =ebex.Security.Cryptography.Ed25519();
//             instance.FromPublicKey(publicKey);
//             return instance.VerifyMessage(message, signature);
//         }
//         catch (Exception e)
//         {
//             return false;
//         }
//     }
// }
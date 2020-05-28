﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Google.Protobuf;
using Shouldly;

namespace AElf.Types.Tests.Extensions
{
    public class ExtensionTests
    {


        [Fact]
        public void Number_Extensions_Methods_Test()
        {
            //ulong
            var uNumber = (ulong)10;
            var byteArray = uNumber.ToBytes();
            byteArray.ShouldNotBe(null);

            //int
            var iNumber = 10;
            var byteArray1 = iNumber.ToBytes();
            byteArray1.ShouldNotBe(null);

            //hash
            var hash = HashHelper.ComputeFrom(iNumber);
            hash.ShouldNotBe(null);
        }

        [Fact]
        public void Byte_Extensions_ToPlainBase58_Test()
        {
            var emptyByteString = ByteString.Empty;
            emptyByteString.ToPlainBase58().ShouldBe(string.Empty);
            
            var byteString = ByteString.CopyFromUtf8("5ta1yvi2dFEs4V7YLPgwkbnn816xVUvwWyTHPHcfxMVLrLB");
            byteString.ToPlainBase58().ShouldBe("SmUQnCq4Ffvy8UeR9EEV9DhNVcNaLhGpqFTDZfzdebANJAgngqe8RfT1sqPPqJQ9");

            var bytes = new byte[] {0, 0, 0};
            byteString = ByteString.CopyFrom(bytes);
            byteString.ToPlainBase58().ShouldBe("111");
        }
    }
}
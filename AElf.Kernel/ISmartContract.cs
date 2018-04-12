﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Protobuf;

namespace AElf.Kernel
{
    
    public interface ISmartContract 
    {
        Task InitializeAsync(IAccountDataProvider dataProvider);
        Task InvokeAsync(IHash caller, 
            string methodname, ByteString bytes);
    }
}
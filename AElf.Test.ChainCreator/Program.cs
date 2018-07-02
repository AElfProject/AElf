﻿using System;
using System.IO;
using AElf.Common.Application;
using AElf.Cryptography.ECDSA.Exceptions;
using AElf.Kernel;
using Google.Protobuf;

namespace AElf.Test.ChainCreator
{
    class Program
    {
        private static string _dataDirectory = ApplicationHelpers.GetDefaultDataDir();
        private const string KeyFileExtension = ".chain";
        private const string KeyFolderName = "tests";
        
        static void Main(string[] args)
        {
            /*BlockFactory fact = new BlockFactory();
            Block b = fact.CreateBlock(Hash.Default);*/
            
            var initializer = new AccountStateInitializer();
            var balances = initializer.Balances(10);
            
            var str = balances.ToString();

            GetOrCreateKeystoreDir();
            var fullpath = GetKeyFileFullPath(DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss"));
            Console.WriteLine(fullpath);
            File.WriteAllText(fullpath, str);
        }
        
        internal static string GetKeyFileFullPath(string time)
        {
            string dirPath = GetKeystoreDirectoryPath();
            string filePath = System.IO.Path.Combine(dirPath, time);
            string filePathWithExtension = System.IO.Path.ChangeExtension(filePath, KeyFileExtension);

            return filePathWithExtension;
        }
        
        internal static DirectoryInfo GetOrCreateKeystoreDir()
        {
            try
            {
                string dirPath = GetKeystoreDirectoryPath();
                return Directory.CreateDirectory(dirPath);
            }
            catch (Exception e)
            {
                throw new KeyStoreNotFoundException("Invalid data directory path", e);
            }
        }

        internal static string GetKeystoreDirectoryPath()
        {
            return System.IO.Path.Combine(_dataDirectory, KeyFolderName);
        }
    }
}
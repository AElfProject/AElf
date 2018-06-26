using System;
using System.IO;
using AElf.Common.Application;
using AElf.Cryptography.ECDSA.Exceptions;
using ServiceStack;

namespace AElf.CLI.Helpers
{
    public class SmartContractReader
    {
        private const string ContractExtension = ".dll";
        private const string ContractFolderName = "contracts";
        
        private readonly string _dataDirectory;

        public SmartContractReader()
        {
            _dataDirectory = ApplicationHelpers.GetDefaultDataDir();
        }

        public byte[] Read(string name)
        {
            byte[] code = null;
            using (FileStream file = File.OpenRead(GetKeyFileFullPath(name)))
            {
                code = file.ReadFully();
            }

            return code;
        }
        
        /// <summary>
        /// Return the full path of the files 
        /// </summary>
        internal string GetKeyFileFullPath(string address)
        {
            string dirPath = GetKeystoreDirectoryPath();
            string filePath = Path.Combine(dirPath, address);
            //string filePathWithExtension = Path.ChangeExtension(filePath, ContractExtension);

            string filePathWithExtension = filePath + ContractExtension;

            return filePathWithExtension;
        }

        internal DirectoryInfo GetOrCreateContractDir()
        {
            try
            {
                string dirPath = GetKeystoreDirectoryPath();
                return Directory.CreateDirectory(dirPath);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        internal string GetKeystoreDirectoryPath()
        {
            return Path.Combine(_dataDirectory, ContractFolderName);
        }
    }
}
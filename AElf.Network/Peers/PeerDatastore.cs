using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NodeData = AElf.Network.Data.NodeData;

namespace AElf.Network.Peers
{
    public class PeerDataStore : IPeerDatabase
    {
        private const string FileName = "peerDB.txt";
        private readonly string _filePath;
        private static string _folderPath;

        public PeerDataStore(string folderPath)
        {
            if (folderPath != null)
            {
                _folderPath = folderPath;
                _filePath = Path.Combine(folderPath, FileName);
            }
        }

        private static bool CheckDbExists()
        {
            FileInfo[] dbFile = null;
            try
            {
                DirectoryInfo di = new DirectoryInfo(_folderPath);
                dbFile = di.GetFiles(FileName);
            }
            catch
            {
                ;
            }

            return dbFile.Length != 0;
        }
        
        public List<NodeData> ReadPeers()
        {
            List<NodeData> peerList = new List<NodeData>();
            
            if (!CheckDbExists()) return peerList; // Returns empty list for robustness. Do empty check during usage
            try
            {
                string[] fileContents = File.ReadAllLines(_filePath);

                foreach (string line in fileContents)
                {
                    string[] sPeer = line.Split(',');

                    NodeData peer = new NodeData
                    {
                        IpAddress = sPeer[0],
                        Port = Convert.ToUInt16(sPeer[1]),
                        IsBootnode = Convert.ToBoolean(sPeer[2])
                    };

                    peerList.Add(peer);
                }
            }
            catch
            {
                ;
            }

            return peerList;
        }

        public void WritePeers(List<NodeData> peerList)
        {
            StringBuilder sb = new StringBuilder();
            string newline;
            foreach (var peer in peerList)
            {
                newline = string.Format($"{peer.IpAddress},{peer.Port},{peer.IsBootnode}");
                sb.AppendLine(newline);
            }

            try
            {
                File.WriteAllText(_filePath, sb.ToString());
            }
            catch
            {
                ;
            }
        }
    }
}
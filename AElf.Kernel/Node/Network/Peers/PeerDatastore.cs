using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AElf.Kernel.Node.Network.Data;

namespace AElf.Kernel.Node.Network.Peers
{
    public class PeerDataStore : IPeerDatabase
    {
        private const string FileName = "peerDB.txt";
        private readonly string _filePath = System.IO.Path.Combine(System.Environment.CurrentDirectory, FileName);
        
        private static bool CheckDbExists()
        {
            DirectoryInfo di = new DirectoryInfo(System.Environment.CurrentDirectory);
            FileInfo[] dbFile = di.GetFiles(FileName);
            return dbFile.Length != 0;
        }
        
        public List<NodeData> ReadPeers()
        {
            List<NodeData> peerList = new List<NodeData>();
            
            if (!CheckDbExists()) return peerList; // Returns empty list for robustness. Do empty check during usage
            string[] fileContents = File.ReadAllLines(_filePath);

            foreach (string line in fileContents)
            {
                string[] sPeer = line.Split(',');
                NodeData peer = new NodeData();
                peer.IpAddress = sPeer[0];
                peer.Port = Convert.ToUInt16(sPeer[1]);
                
                peerList.Add(peer);
            }

            return peerList;
        }

        public void WritePeers(List<NodeData> peerList)
        {
            StringBuilder sb = new StringBuilder();
            string newline;
            foreach (var peer in peerList)
            {
                newline = string.Format($"{peer.IpAddress},{peer.Port}");
                sb.AppendLine(newline);
            }
            
            File.WriteAllText(_filePath, sb.ToString());
        }
    }
}
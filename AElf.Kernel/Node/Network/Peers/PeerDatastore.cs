using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
        
        public List<IPeer> ReadPeers()
        {
            List<IPeer> peerList = new List<IPeer>();
            
            if (!CheckDbExists()) return peerList; // Returns empty list for robustness. Do empty check during usage
            string[] fileContents = File.ReadAllLines(_filePath);

            foreach (string line in fileContents)
            {
                string[] sPeer = line.Split(',');
                Peer peer = new Peer(null, sPeer[0], Convert.ToUInt16(sPeer[1]));
               
                // Change above line to below code when switch from
                // Peer to NodeData is complete. Change interface to reflect
                // as well as method signatures
//                NodeData peer = new NodeData()
//                {
//                    IpAddress = sPeer[0],
//                    Port = Convert.ToUInt16(sPeer[1])
//                };
                
                peerList.Add(peer);
            }

            return peerList;
        }

        public void WritePeers(List<IPeer> peerList)
        {
            StringBuilder sb = new StringBuilder();
            string newline;
            foreach (IPeer peer in peerList)
            {
                newline = string.Format($"{peer.IpAddress},{peer.Port}");
                sb.AppendLine(newline);
            }
            
            File.WriteAllText(_filePath, sb.ToString());
        }
    }
}
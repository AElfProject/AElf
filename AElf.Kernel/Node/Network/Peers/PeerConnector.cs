using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace AElf.Kernel.Node.Network.Peers
{
    public class PeerConnector
    {
        private const int PeerBufferLength = 1024;
        
        private Peer _distantPeer;
        private TcpClient _connection;
        private NetworkStream _stream;
        
        public bool IsConnected { get; set; }

        public PeerConnector(TcpClient connection)
        {
            _connection = connection;
            _stream = connection.GetStream();
        }

        /// <summary>
        /// Reads the data send by the remote peer after a connection
        /// request.
        /// </summary>
        /// <returns></returns>
        public async Task FinalizeConnect()
        {
            try
            {
                byte[] bytes = new byte[PeerBufferLength];
                int bytesRead = await _stream.ReadAsync(bytes, 0, 1024);

                byte[] readBytes = new byte[bytesRead];
                Array.Copy(bytes, readBytes, bytesRead);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
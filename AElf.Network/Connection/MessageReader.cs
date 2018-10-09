using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Network.Data;
using AElf.Network.Exceptions;
using Google.Protobuf;
using NLog;

namespace AElf.Network.Connection
{
    public class PacketReceivedEventArgs : EventArgs
    {
        public Message Message { get; set; }
    }
    
    public class MessageReader : IMessageReader
    {   
        private const int IntLength = 4;
        private const int IdLength = 16;

        private ILogger _logger;
        
        private readonly NetworkStream _stream;

        public event EventHandler PacketReceived;
        public event EventHandler StreamClosed;

        private readonly List<PartialPacket> _partialPacketBuffer;

        public bool IsConnected { get; private set; }
        
        public MessageReader(NetworkStream stream)
        {
            _partialPacketBuffer = new List<PartialPacket>();
            
            _stream = stream;
        }
        
        public void Start()
        {
            Task.Run(Read).ConfigureAwait(false);
            IsConnected = true;

            _logger = LogManager.GetLogger(nameof(MessageReader));
        }
        
        /// <summary>
        /// Reads the bytes from the stream.
        /// </summary>
        private async Task Read()
        {
            try
            {
                while (true)
                {
                    // Read type 
                    int type = await ReadByte();
                    
                    // Read if the message is associated with an id
                    bool hasId = await ReadBoolean();

                    byte[] id = null;
                    if (hasId)
                    {
                        // The Id is a 128-bit guid
                        id = await ReadBytesAsync(IdLength);
                    }
                    
                    // Is this a partial reception ?
                    bool isBuffered = await ReadBoolean();

                    // Read the size of the data
                    int length = await ReadInt();

                    if (isBuffered)
                    {
                        // If it's a partial packet read the packet info
                        PartialPacket partialPacket = await ReadPartialPacket(length);

                        // todo property control

                        if (!partialPacket.IsEnd)
                        {
                            _partialPacketBuffer.Add(partialPacket);
                            _logger.Trace($"Received packet : {(MessageType)type}, length : {length}");
                        }
                        else
                        {
                            // This is the last packet
                            // Concat all data 

                            _partialPacketBuffer.Add(partialPacket);

                            byte[] allData =
                                ByteArrayHelpers.Combine(_partialPacketBuffer.Select(pp => pp.Data).ToArray());

                            _logger.Trace($"Received last packet : {_partialPacketBuffer.Count}, total length : {allData.Length}");

                            // Clear the buffer for the next partial to receive 
                            _partialPacketBuffer.Clear();
                            
                            Message message;
                            if (hasId)
                            {
                                message = new Message {Type = type, HasId = true, Id = id, Length = allData.Length, Payload = allData};
                            }
                            else
                            {
                                message = new Message {Type = type, HasId = false, Length = allData.Length, Payload = allData};
                            }
                            
                            FireMessageReceivedEvent(message);
                        }
                    }
                    else
                    {
                        // If it's not a partial packet the next "length" bytes should be 
                        // the entire data

                        byte[] packetData = await ReadBytesAsync(length);

                        Message message;
                        if (hasId)
                        {
                            message = new Message {Type = type, HasId = true, Id = id, Length = length, Payload = packetData};
                        }
                        else
                        {
                            message = new Message {Type = type, HasId = false, Length = length, Payload = packetData};
                        }
                        
                        FireMessageReceivedEvent(message);
                    }
                }
            }
            catch (PeerDisconnectedException e)
            {
                StreamClosed?.Invoke(this, EventArgs.Empty);
                Close();
            }
            catch (Exception e)
            {
                if (!IsConnected && e is IOException)
                {
                    // If the stream fails while the connection is logically closed (call to Close())
                    // we simply return - the StreamClosed event will no be closed.
                    return;
                }

                Close();
                StreamClosed?.Invoke(this, EventArgs.Empty);
            }
        }

        private void FireMessageReceivedEvent(Message message)
        {
            PacketReceivedEventArgs args = new PacketReceivedEventArgs { Message = message };
            PacketReceived?.Invoke(this, args);
        }

        private async Task<int> ReadByte()
        {
            byte[] type = await ReadBytesAsync(1);
            return type[0];
        }

        private async Task<int> ReadInt()
        {
            byte[] intBytes = await ReadBytesAsync(IntLength);
            return BitConverter.ToInt32(intBytes, 0);
        }

        private async Task<bool> ReadBoolean()
        {
            byte[] isBuffered = await ReadBytesAsync(1);
            return isBuffered[0] != 0;
        }

        private async Task<PartialPacket> ReadPartialPacket(int dataLength)
        {
            PartialPacket partialPacket = new PartialPacket();

            partialPacket.Position = await ReadInt();
            partialPacket.IsEnd = await ReadBoolean();
            partialPacket.TotalDataSize = await ReadInt();
            
            // Read the data
            byte[] packetData = await ReadBytesAsync(dataLength);
            partialPacket.Data = packetData;
            
            return partialPacket;
        }
        
        /// <summary>
        /// Reads bytes from the stream.
        /// </summary>
        /// <param name="amount">The amount of bytes we want to read.</param>
        /// <returns>The read bytes.</returns>
        protected async Task<byte[]> ReadBytesAsync(int amount)
        {
            if (amount == 0)
            {
                _logger.Trace("Read amount is 0");
                return new byte[0];
            }
            
            byte[] requestedBytes = new byte[amount];
            
            int receivedIndex = 0;
            while (receivedIndex < amount)
            {
                int readAmount = await _stream.ReadAsync(requestedBytes, receivedIndex, amount - receivedIndex);
                
                if (readAmount == 0)
                    throw new PeerDisconnectedException();
                
                receivedIndex += readAmount;
            }
            
            return requestedBytes;
        }

        #region Closing and disposing

        public void Close()
        {
            Dispose();
        }
        
        public void Dispose()
        {
            // Change logical connection state
            IsConnected = false;
            
            // This will cause an IOException in the read loop
            // but since IsConnected is switched to false, it 
            // will not fire the disconnection exception.
            _stream?.Close();
        }

        #endregion
    }
}
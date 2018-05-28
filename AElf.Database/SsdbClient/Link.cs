using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("AElf.Database.Tests")]

namespace AElf.Database.SsdbClient
{
	internal class Link:IDisposable
	{
		private TcpClient _sock;
		private readonly string _host;
		private readonly int _port;
		private MemoryStream _recvBuf = new MemoryStream(8 * 1024);

		public Link(string host, int port)
		{
			_host = host;
			_port = port;
		}

		public bool Connect()
		{
			_sock = new TcpClient(_host, _port);
			_sock.NoDelay = true;
			_sock.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
			return true;
		}

		public void Dispose()
		{
			Close();
		}

		public void Close() 
		{
			_sock?.Close();
			_sock = null;
		}

		public List<byte[]> Request(string cmd, params string[] args) 
		{
			var req = new List<byte[]>(1 + args.Length);
			req.Add(Helper.StringToBytes(cmd));
			foreach(var s in args) 
			{
				req.Add(Helper.StringToBytes(s));
			}
			return Request(req);
		}

		public List<byte[]> Request(string cmd, params byte[][] args) 
		{
			var req = new List<byte[]>(1 + args.Length);
			req.Add(Helper.StringToBytes(cmd));
			req.AddRange(args);
			return Request(req);
		}

		public List<byte[]> Request(List<byte[]> req) 
		{
			var buf = new MemoryStream();
			foreach(var p in req) 
			{
				var len = Helper.StringToBytes(p.Length.ToString());
				buf.Write(len, 0, len.Length);
				buf.WriteByte((byte)'\n');
				buf.Write(p, 0, p.Length);
				buf.WriteByte((byte)'\n');
			}
			buf.WriteByte((byte)'\n');

			var bs = buf.GetBuffer();
			_sock.GetStream().Write(bs, 0, (int)buf.Length);
			//Console.Write(Encoding.Default.GetString(bs, 0, (int)buf.Length));
			return Recv();
		}

		private List<byte[]> Recv() 
		{
			while(true) 
			{
				var ret = Parse();
				if(ret != null) 
				{
					return ret;
				}
				var bs = new byte[8192];
				var len = _sock.GetStream().Read(bs, 0, bs.Length);
				//Console.WriteLine("<< " + Encoding.Default.GetString(bs));
				_recvBuf.Write(bs, 0, len);
			}
		}

		private List<byte[]> Parse() 
		{
			var list = new List<byte[]>();
			var buf = _recvBuf.GetBuffer();

			var idx = 0;
			while(true) 
			{
				var pos = Helper.Memchr(buf, (byte)'\n', idx);
				//System.out.println("pos: " + pos + " idx: " + idx);
				if(pos == -1) 
				{
					break;
				}
				if(pos == idx || (pos == idx + 1 && buf[idx] == '\r')) 
				{
					idx += 1; // if '\r', next time will skip '\n'
					// ignore empty leading lines
					if(list.Count == 0) 
					{
						continue;
					}
					var left = (int)_recvBuf.Length - idx;
					_recvBuf = new MemoryStream(8192);
					if(left > 0) {
						_recvBuf.Write(buf, idx, left);
					}
					return list;
				}
				var lens = new byte[pos - idx];
				Array.Copy(buf, idx, lens, 0, lens.Length);
				var len = Int32.Parse(Helper.BytesToString(lens));

				idx = pos + 1;
				if(idx + len >= _recvBuf.Length) 
				{
					break;
				}
				var data = new byte[len];
				Array.Copy(buf, idx, data, 0, (int)data.Length);

				//Console.WriteLine("len: " + len + " data: " + Encoding.Default.GetString(data));
				idx += len + 1; // skip '\n'
				list.Add(data);
			}
			return null;
		}
	}
}
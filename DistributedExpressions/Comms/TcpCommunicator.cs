using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using DistributedExpressions.Utils;
using DistributedExpressions.Model;

namespace DistributedExpressions.Comms
{
    public class TcpCommunicator : ITcpCommunicator
    {
        public static readonly int DefaultPort = 24816;

        private bool _listening;
        private TcpListener _listener;
        private IReportingOperation _listenerThread;

        public bool NoDelay { get { return _listener.Server.NoDelay; } set { _listener.Server.NoDelay = value; } }
        public bool IsListening { get { return _listening; } }
        public long ElapsedListeningMs { get { return _listenerThread.ElapsedMs; } }

        public bool IsLocal { get { return PublicIp == null; } }
        public IPAddress PublicIp { get; private set; }
        public IPEndPoint PublicEndPoint { get { return new IPEndPoint(PublicIp, Port); } }
        public IPEndPoint LocalEndPoint { get { return _listener.LocalEndpoint as IPEndPoint; } }
        public IPAddress LocalIp { get { return LocalEndPoint.Address; } }
        public int Port { get { return LocalEndPoint.Port; } }

        public ILocalVariables LocalVariables { get; set; }

        public event EventHandler<CommsEventArgs> Received;
        public event EventHandler<CommsEventArgs> Sent;

        public TcpCommunicator() : this(true, DefaultPort, null)
        { }

        public TcpCommunicator(int port) : this(true, port, null)
        { }

        public TcpCommunicator(ILocalVariables localVariables) : this(true, DefaultPort, localVariables)
        { }

        public TcpCommunicator(int port, ILocalVariables localVariables) : this(true, port, localVariables)
        { }

        public TcpCommunicator(bool local) : this(local, DefaultPort, null)
        { }

        public TcpCommunicator(bool local, int port) : this(local, port, null)
        { }

        public TcpCommunicator(bool local, ILocalVariables localVariables) : this(local, DefaultPort, localVariables)
        { }

        public TcpCommunicator(bool local, int port, ILocalVariables localVariables)
        {
            _listening = false;
            LocalVariables = localVariables;
            _listenerThread = new ReportingThread();

            if (local)
            {
                PublicIp = null;
                _listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
            }
            else
            {
                PublicIp = IPAddress.Parse(new WebClient().DownloadString("https://api.ipify.org/"));
                var h = Dns.GetHostEntry(Dns.GetHostName());
                IPAddress host = null;
                foreach (var ip in h.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        host = ip;
                        break;
                    }
                }
                _listener = new TcpListener(host, port);
            }
        }

        public void Open()
        {
            _listener.Start();
            _listening = true;
            _listenerThread.Cancelled += Close_Event;
            _listenerThread.Reported += Reported_Event;

            _listenerThread.Run(() =>
            {
                while (_listening)
                {
                    if (!_listenerThread.Cancel)
                    {
                        if (!_listener.Pending())
                        {
                            Thread.Sleep(1);
                            continue;
                        }
                        ReadAvailable(_listener.AcceptTcpClient());
                    }
                    else break;
                }
            });
        }

        public void Close()
        {
            _listenerThread.Cancel = true;
        }

        public void Send(VarPacket packet)
        {
            Task.Run(() =>
            {
                CommsEventArgs args = null;
                try
                {
                    using (var client = new TcpClient(packet.EndPoint.Address.ToString(), packet.EndPoint.Port) { NoDelay = this.NoDelay })
                    using (var s = client.GetStream())
                    {
                        s.Write(packet.Data, 0, packet.Data.Length);
                        s.Flush();

                        args = new CommsEventArgs(packet, LocalEndPoint, null);
                        _listenerThread.Report(Tuple.Create(args, true, (VarPacket)null));

                        if (packet.IsReadRequest)
                        {
                            var data = _ReadPacketData(s);
                            VarPacket p = null;

                            try
                            {
                                p = VarPacket.CreateReceived(this, LocalEndPoint, data);
                            }
                            catch (ArgumentException ex)
                            {
                                args = new CommsEventArgs(p, client.Client.RemoteEndPoint as IPEndPoint, ex.Message);
                                _listenerThread.Report(Tuple.Create(args, false, (VarPacket)null));
                                return;
                            }

                            args = new CommsEventArgs(p, client.Client.RemoteEndPoint as IPEndPoint, p.IsReadResponse ? null : "Wrong packet type received.");
                            _listenerThread.Report(Tuple.Create(args, false, (VarPacket)null));
                        }
                    }
                }
                catch (Exception ex)
                {
                    args = new CommsEventArgs(packet, LocalEndPoint, ex.Message);
                    _listenerThread.Report(Tuple.Create(args, true, (VarPacket)null));
                }
            });
        }

        private byte[] _ReadPacketData(NetworkStream ns)
        {
            int i = 0;
            var len = new byte[4];

            while (i < len.Length)
                i += ns.Read(len, i, len.Length - i);

            var data = new byte[BitConverter.ToInt32(len, 0)];
            data.WriteAt(len, 0);

            while (i < data.Length)
                i += ns.Read(data, i, data.Length - i);

            return data;
        }

        private void ReadAvailable(TcpClient client)
        {
            Task.Run(() =>
            {
                using (var s = client.GetStream())
                {
                    CommsEventArgs args = null;
                    var data = _ReadPacketData(s);
                    VarPacket p = null;

                    try
                    {
                        p = VarPacket.CreateReceived(this, LocalEndPoint, data);
                    }
                    catch (ArgumentException ex)
                    {
                        args = new CommsEventArgs(p, client.Client.RemoteEndPoint as IPEndPoint, ex.Message);
                        _listenerThread.Report(Tuple.Create(args, false, (VarPacket)null));
                        return;
                    }

                    args = new CommsEventArgs(p, client.Client.RemoteEndPoint as IPEndPoint, null);
                    _listenerThread.Report(Tuple.Create(args, false, p.IsWriteRequest ? p : null));

                    if (p.IsReadRequest)
                        _RespondToRequest(p, client.Client.RemoteEndPoint as IPEndPoint, s);
                }
            });
        }

        private void _RespondToRequest(VarPacket packet, IPEndPoint endPoint, NetworkStream ns)
        {
            bool isok = true;
            var names = packet.RequestVarNames;
            var values = new List<decimal>(names.Length);

            foreach (var n in names)
            {
                Variable<decimal> v;
                if (!LocalVariables.TryGet(n, out v))
                {
                    isok = false;
                    break;
                }
                values.Add(v.Value);
            }

            var p = VarPacket.CreateReadResponse(this, endPoint, isok ? values : null);
            ns.Write(p.Data, 0, p.Data.Length);

            var args = new CommsEventArgs(p, LocalEndPoint, null);
            _listenerThread.Report(Tuple.Create(args, true, (VarPacket)null));
        }

        protected virtual void OnReceived(CommsEventArgs e)
        {
            if (Received != null) Received(this, e);
        }

        protected virtual void OnSent(CommsEventArgs e)
        {
            if (Sent != null) Sent(this, e);
        }

        private void Reported_Event(object sender, ReportedEventArgs e)
        {
            var args = e.Parameter as Tuple<CommsEventArgs, bool, VarPacket>;

            if (args.Item2) OnSent(args.Item1);
            else
            {
                if (args.Item3 != null)
                {
                    var names = args.Item3.RequestVarNames;
                    var value = args.Item3.WriteRequestValue.Value;

                    foreach (var n in names)
                        LocalVariables.Set(new Variable<decimal>(n, value));
                }
                OnReceived(args.Item1);
            }
        }

        private void Close_Event(object sender, EventArgs e)
        {
            _listening = false;
            _listener.Stop();
            _listenerThread.Cancelled -= Close_Event;
            _listenerThread.Reported -= Reported_Event;
        }

        public void Dispose()
        {
            Close();
        }
    }
}

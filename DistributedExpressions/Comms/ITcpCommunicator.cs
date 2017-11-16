using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace DistributedExpressions.Comms
{
    public interface ITcpCommunicator : IDisposable
    {
        event EventHandler<CommsEventArgs> Sent;
        event EventHandler<CommsEventArgs> Received;

        bool IsLocal { get; }
        IPAddress PublicIp { get; }
        IPEndPoint PublicEndPoint { get; }
        IPEndPoint LocalEndPoint { get; }
        IPAddress LocalIp { get; }
        int Port { get; }
        bool IsListening { get; }
        long ElapsedListeningMs { get; }

        void Open();
        void Close();
        void Send(VarPacket packet);
    }
}

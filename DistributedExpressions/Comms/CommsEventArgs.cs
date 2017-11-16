using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace DistributedExpressions.Comms
{
    public class CommsEventArgs : EventArgs
    {
        public VarPacket Packet { get; private set; }
        public IPEndPoint Source { get; private set; }
        public bool Ok { get { return Msg == null; } }
        public string Msg { get; private set; }

        public CommsEventArgs(VarPacket packet, IPEndPoint source, string msg)
        {
            Packet = packet;
            Source = source;
            Msg = msg;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Diagnostics;
using DistributedExpressions.Utils;
using DistributedExpressions.Comms;

namespace DistributedExpressions.Model
{
    public class StatementExecutor : IDisposable
    {
        private List<string> _localAssignees;
        private List<string> _localVariables;
        private Dictionary2D<IPAddress, string> _remoteAssignees;
        private Dictionary2D<IPAddress, string> _remoteVariables;
        private MathStatement _ms;

        private List<VarPacket> _receivedVars;
        private List<string> _errors;

        private bool _nan;
        private bool _cancel;
        public bool Cancel { get { return _cancel; } set { if (IsRunning) _cancel = value; } }
        public bool IsRunning { get; private set; }

        public string[] Errors { get { return (_errors == null || _errors.Count == 0) ? null : _errors.ToArray(); } }
        public string Result { get; private set; }
        public bool Successful { get { return Result != null && !Result.Equals("NaN"); } }

        private ITcpCommunicator _communicator;
        public ITcpCommunicator Communicator
        {
            get { return _communicator; }
            set
            {
                if (_communicator != value)
                {
                    if (_communicator != null)
                    {
                        _communicator.Sent -= PacketSent_Event;
                        _communicator.Received -= PacketReceived_Event;
                    }
                    _communicator = value;
                    _communicator.Sent += PacketSent_Event;
                    _communicator.Received += PacketReceived_Event;
                }
            }
        }

        public ILocalVariables LocalVariables { get; set; }

        public string MathStatement { get { return _ms.Input; } }

        public int RemoteHostCount { get { return _remoteAssignees.Count + _remoteVariables.Count; } }

        public int LocalAssigneeCount { get { return _localAssignees.Count; } }
        public int LocalVarCount { get { return _localVariables.Count; } }
        public int RemoteAssigneeCount { get { return _remoteAssignees.CountAll(); } }
        public int RemoteVarCount { get { return _remoteVariables.CountAll(); } }

        public int AssigneeCount { get { return _localAssignees.Count + _remoteAssignees.CountAll(); } }
        public int VarCount { get { return _localVariables.Count + _remoteVariables.CountAll(); } }
        public int LocalArgCount { get { return _localAssignees.Count + _localVariables.Count; } }
        public int RemoteArgCount { get { return _remoteAssignees.CountAll() + _remoteVariables.CountAll(); } }

        public int ArgCount
        {
            get { return _localAssignees.Count + _localVariables.Count + _remoteAssignees.CountAll() + _remoteVariables.CountAll(); }
        }

        public StatementExecutor(List<VariableInfo> variables, MathStatement statement) : this(variables, statement, null, null)
        { }

        public StatementExecutor(List<VariableInfo> variables, MathStatement statement, ITcpCommunicator communicator, ILocalVariables localVariables)
        {
            IsRunning = false;
            _cancel = false;
            _nan = false;
            Communicator = communicator;
            LocalVariables = localVariables;

            _localAssignees = new List<string>();
            _localVariables = new List<string>();
            _remoteAssignees = new Dictionary2D<IPAddress, string>();
            _remoteVariables = new Dictionary2D<IPAddress, string>();
            _ms = statement;

            foreach (var v in variables)
            {
                if (v.IsAssignee)
                {
                    if (v.IsRemote) _remoteAssignees.Add(v.Ip, v.LocalName);
                    else _localAssignees.Add(v.LocalName);
                }
                else if (v.IsRemote) _remoteVariables.Add(v.Ip, v.LocalName);
                else _localVariables.Add(v.LocalName);
            }
        }

        public void Clear()
        {
            _cancel = false;
            _nan = false;
            _errors = null;
            Result = null;
        }

        public string Execute()
        {
            IsRunning = true;
            Clear();
            _errors = new List<string>();

            if (LocalVariables == null || Communicator == null)
                _errors.Add("No local variables or communicator specified.");
            else
            {
                var vals = _MapArgValues();
                if (vals != null)
                {
                    try
                    {
                        var r = _ms.Compute(vals);
                        Result = r.ToString();
                        _AssignResult(r);
                    }
                    catch (Exception ex)
                    {
                        _errors.Add(ex.Message);
                    }
                }
            }
            if (_nan) Result = "NaN";
            IsRunning = false;
            return Result;
        }

        private decimal[] _MapArgValues()
        {
            var vals = new decimal[VarCount];
            if (vals.Length > 0)
            {
                foreach (var s in _localVariables)
                {
                    Variable<decimal> v;
                    if (!LocalVariables.TryGet(s, out v))
                    {
                        _nan = true;
                        return null;
                    }
                    vals[_ms.ArgIndex(v.Name)] = v.Value;
                }

                if (_remoteVariables.Count > 0)
                {
                    int i = 0;
                    var packets = new VarPacket[_remoteVariables.Count];
                    foreach (var vs in _remoteVariables)
                        packets[i++] = VarPacket.CreateReadRequest(this, new IPEndPoint(vs.Key, _communicator.Port), vs.Value);

                    _receivedVars = new List<VarPacket>(packets.Length);

                    foreach (var p in packets)
                        _communicator.Send(p);

                    while (_receivedVars.Count < packets.Length && _errors.Count == 0 && !_nan)
                    {
                        Thread.Sleep(1);
                        if (_cancel)
                        {
                            _errors.Add("Cancelled.");
                            return null;
                        }
                    }

                    if (_errors.Count > 0 || _nan) return null;

                    for (i = 0; i < packets.Length; ++i)
                    {
                        if (!_receivedVars[i].IsReadResponseOk)
                        {
                            _nan = true;
                            _errors.Add("Host at " + packets[i].EndPoint + " returned NaN.");
                            return null;
                        }
                        var ip = packets[i].EndPoint.Address;
                        var names = packets[i].RequestVarNames;
                        var values = _receivedVars[i].ReadResponseVarValues;
                        for (int j = 0; j < names.Length; ++j)
                            vals[_ms.ArgIndex("{" + ip.ToString() + "}." + names[j])] = values[j];
                    }
                    _receivedVars = null;
                }
            }
            return vals;
        }

        private void _AssignResult(decimal result)
        {
            if (AssigneeCount > 0)
            {
                foreach (var s in _localAssignees)
                    LocalVariables.Set(new Variable<decimal>(s, result));

                if (_remoteAssignees.Count > 0)
                {
                    int i = 0;
                    var packets = new VarPacket[_remoteAssignees.Count];
                    foreach (var vs in _remoteAssignees)
                        packets[i++] = VarPacket.CreateWriteRequest(this, new IPEndPoint(vs.Key, _communicator.Port), vs.Value, result);

                    foreach (var p in packets)
                        _communicator.Send(p);
                }
            }
        }

        private void PacketSent_Event(object sender, CommsEventArgs e)
        {   // owner probably not required
            if (e.Packet.Owner == this && IsRunning && !e.Ok && e.Packet.IsReadRequest)
                _errors.Add(e.Msg);
        }

        private void PacketReceived_Event(object sender, CommsEventArgs e)
        {
            if (IsRunning && e.Packet.IsReadResponse)     // packet owner? would have to be sent with the packet (VarPacket.OwnerId as int instead of object?)
            {                                             // also, gc heap compression makes it tricky, however, statement executions should happen one at a time on a host
                _receivedVars.Add(e.Packet);
                if (!e.Ok) _errors.Add(e.Msg);
                else if (!e.Packet.IsReadResponseOk)
                    _nan = true;
            }
        }

        private StatementExecutor()
        { }

        public void Dispose()
        {
            if (_communicator != null)
            {
                _communicator.Sent -= PacketSent_Event;
                _communicator.Received -= PacketReceived_Event;
            }
        }
    }
}

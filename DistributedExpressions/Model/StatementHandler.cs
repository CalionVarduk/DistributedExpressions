using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DistributedExpressions.Utils;
using DistributedExpressions.Comms;

namespace DistributedExpressions.Model
{
    public class StatementHandler : IDisposable
    {
        private StatementExecutor _executor;
        private StatementHandledEventArgs _args;
        private IReportingOperation _thread;

        public ITcpCommunicator Communicator { get; set; }
        public ILocalVariables LocalVariables { get; set; }
        public bool IsRunning { get { return _thread.IsRunning; } }
        public bool Cancel { get { return _executor != null && _executor.Cancel; } set { if (_executor != null) _executor.Cancel = value; } }
        public long ElapsedMs { get { return _thread.ElapsedMs; } }

        public event EventHandler<StatementHandledEventArgs> StatementHandled;

        public StatementHandler(ITcpCommunicator communicator, ILocalVariables localVariables)
        {
            if (communicator == null || localVariables == null) throw new ArgumentNullException();
            Communicator = communicator;
            LocalVariables = localVariables;

            _thread = new ReportingThread();
            _thread.Completed += Completed_Event;
        }

        public void Handle(string statement)
        {
            _thread.Run(() =>
            {
                var parser = new StatementParser(statement);
                if (parser.Successful)
                {
                    using (_executor = parser.Executor)
                    {
                        _executor.LocalVariables = LocalVariables;
                        _executor.Communicator = Communicator;
                        _executor.Execute();
                        _args = new StatementHandledEventArgs(_executor.Result, _executor.Errors, _executor.Cancel);
                    }
                    _executor = null;
                }
                else _args = new StatementHandledEventArgs(null, new string[] { parser.Error }, false);
            });
        }

        private void Completed_Event(object sender, EventArgs e)
        {
            OnStatementHandled(_args);
        }

        protected virtual void OnStatementHandled(StatementHandledEventArgs e)
        {
            if (StatementHandled != null) StatementHandled(this, e);
        }

        private StatementHandler()
        { }

        public void Dispose()
        {
            _thread.Completed -= Completed_Event;
        }
    }
}

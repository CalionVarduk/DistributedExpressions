using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;

namespace DistributedExpressions.Utils
{
    public class ReportingThread : IReportingOperation, IDisposable
    {
        private Stopwatch _watch;
        private BackgroundWorker _worker;
        private Action _action;

        private bool _cancel;
        public bool Cancel { get { return _cancel; } set { if (IsRunning) _cancel = value; } }
        public bool IsRunning { get { return _worker.IsBusy; } }
        public long ElapsedMs { get { return _watch.ElapsedMilliseconds; } }

        public event EventHandler<ReportedEventArgs> Reported;
        public event EventHandler Cancelled;
        public event EventHandler Completed;

        public ReportingThread()
        {
            _watch = new Stopwatch();
            _worker = new BackgroundWorker();
            _worker.WorkerReportsProgress = true;
            _worker.DoWork += _DoWork;
            _worker.RunWorkerCompleted += _WorkDone;
            _worker.ProgressChanged += _Report;
        }

        public void Report()
        {
            _worker.ReportProgress(0);
        }

        public void Report(object parameter)
        {
            _worker.ReportProgress(0, parameter);
        }

        public void Run(Action action)
        {
            if (_worker.IsBusy) throw new InvalidOperationException("Thread is already running.");
            _action = action;
            _watch.Restart();
            _worker.RunWorkerAsync();
        }

        private void _DoWork(object sender, DoWorkEventArgs e)
        {
            _action();
        }

        private void _Report(object sender, ProgressChangedEventArgs e)
        {
            OnReported(new ReportedEventArgs(e.UserState));
        }

        private void _WorkDone(object sender, RunWorkerCompletedEventArgs e)
        {
            _watch.Stop();
            if (_cancel)
            {
                OnCancelled(EventArgs.Empty);
                _cancel = false;
            }
            else OnCompleted(EventArgs.Empty);
            _action = null;
        }

        protected virtual void OnReported(ReportedEventArgs e)
        {
            if (Reported != null) Reported(this, e);
        }

        protected virtual void OnCancelled(EventArgs e)
        {
            if (Cancelled != null) Cancelled(this, e);
        }

        protected virtual void OnCompleted(EventArgs e)
        {
            if (Completed != null) Completed(this, e);
        }

        public void Dispose()
        {
            _worker.DoWork -= _DoWork;
            _worker.RunWorkerCompleted -= _WorkDone;
            _worker.ProgressChanged -= _Report;
            _worker.Dispose();
        }
    }

    public class ReportingThread<T> : ReportingThread
    {
        public T Arg { get; set; }

        public ReportingThread() : base()
        { }
    }
}

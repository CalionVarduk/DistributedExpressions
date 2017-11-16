using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Diagnostics;

namespace DistributedExpressions.Utils
{
    public abstract class ReportingInternalThread : IReportingInternalOperation, IDisposable
    {
        private Stopwatch _watch;
        private BackgroundWorker _worker;

        private bool _cancel;
        public bool Cancel { get { return _cancel; } set { if (IsRunning) _cancel = value; } }
        public bool IsRunning { get { return _worker.IsBusy; } }
        public long ElapsedMs { get { return _watch.ElapsedMilliseconds; } }

        public event EventHandler<ReportedEventArgs> Reported;
        public event EventHandler Cancelled;
        public event EventHandler Completed;

        protected ReportingInternalThread()
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

        public void Run()
        {
            if (_worker.IsBusy) throw new InvalidOperationException("Thread is already running.");
            _watch.Restart();
            _worker.RunWorkerAsync();
        }

        private void _DoWork(object sender, DoWorkEventArgs e)
        {
            OnRun();
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
        }

        protected abstract void OnRun();

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

    public abstract class ReportingInternalThread<T> : ReportingInternalThread
    {
        public T Arg { get; set; }

        protected ReportingInternalThread()
            : base()
        { }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace DistributedExpressions.Utils
{
    public class ReportingTask : IReportingOperation
    {
        private Stopwatch _watch;
        private Task _task;
        private object _reportParameter;

        public TaskScheduler Scheduler { get; private set; }
        public TaskScheduler CallbackScheduler { get; private set; }

        private bool _cancel;
        public bool Cancel { get { return _cancel; } set { if (IsRunning) _cancel = value; } }
        public bool IsRunning { get { return _task != null && _task.Status == TaskStatus.Running; } }
        public long ElapsedMs { get { return _watch.ElapsedMilliseconds; } }

        public event EventHandler<ReportedEventArgs> Reported;
        public event EventHandler Cancelled;
        public event EventHandler Completed;

        public ReportingTask() : this(TaskScheduler.Default, TaskScheduler.Current)
        { }

        public ReportingTask(TaskScheduler scheduler) : this(scheduler, TaskScheduler.Current)
        { }

        public ReportingTask(TaskScheduler scheduler, TaskScheduler callbackScheduler)
        {
            _watch = new Stopwatch();
            Scheduler = scheduler;
            CallbackScheduler = callbackScheduler;
        }

        public void Report()
        {
            Task.Factory.StartNew(_PrepareReport, System.Threading.CancellationToken.None, TaskCreationOptions.DenyChildAttach, CallbackScheduler);
        }

        public void Report(object parameter)
        {
            _reportParameter = parameter;
            Report();
        }

        public void Run(Action action)
        {
            if (IsRunning) throw new InvalidOperationException("Task is already running.");
            _watch.Restart();
            _task = Task.Factory.StartNew(action, System.Threading.CancellationToken.None, TaskCreationOptions.DenyChildAttach, Scheduler).
                    ContinueWith(_PrepareCallback, CallbackScheduler);
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

        private void _PrepareReport()
        {
            OnReported(new ReportedEventArgs(_reportParameter));
            _reportParameter = null;
        }

        private void _PrepareCallback(Task cTask)
        {
            _watch.Stop();
            _task = null;

            if (_cancel)
            {
                OnCancelled(EventArgs.Empty);
                _cancel = false;
            }
            else OnCompleted(EventArgs.Empty);
        }
    }

    public class ReportingTask<T> : ReportingTask
    {
        public T Arg { get; set; }

        public ReportingTask() : base()
        { }

        public ReportingTask(TaskScheduler scheduler) : base(scheduler)
        { }

        public ReportingTask(TaskScheduler scheduler, TaskScheduler callbackScheduler) : base(scheduler, callbackScheduler)
        { }
    }
}

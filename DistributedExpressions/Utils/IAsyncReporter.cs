using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedExpressions.Utils
{
    public interface IAsyncReporter
    {
        bool Cancel { get; set; }
        bool IsRunning { get; }
        long ElapsedMs { get; }

        event EventHandler<ReportedEventArgs> Reported;
        event EventHandler Cancelled;
        event EventHandler Completed;

        void Report();
        void Report(object parameter);
    }
}

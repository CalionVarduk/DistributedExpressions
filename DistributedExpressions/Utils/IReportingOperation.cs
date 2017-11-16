using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedExpressions.Utils
{
    public interface IReportingOperation : IAsyncReporter
    {
        void Run(Action action);
    }
}
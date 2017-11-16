using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedExpressions.Utils
{
    public class ReportedEventArgs : EventArgs
    {
        public object Parameter { get; private set; }
        public bool HasParameter { get { return Parameter != null; } }

        public ReportedEventArgs(object parameter)
        {
            Parameter = parameter;
        }
    }
}

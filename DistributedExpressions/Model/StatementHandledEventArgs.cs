using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedExpressions.Model
{
    public class StatementHandledEventArgs : EventArgs
    {
        public string Result { get; private set; }
        public string[] Errors { get; private set; }
        public bool Successful { get { return Result != null && !Result.Equals("NaN"); } }
        public bool Cancelled { get; private set; }
        public bool Nan { get { return Result != null && Result.Equals("NaN"); } }

        public StatementHandledEventArgs(string result, string[] errors, bool cancelled)
        {
            Result = result;
            Errors = errors;
            Cancelled = cancelled;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedExpressions.Model
{
    public interface ILocalVariables : IEnumerable<Variable<decimal>>
    {
        decimal this[string name] { get; }

        int Count { get; }

        bool Exists(string name);

        bool Set(Variable<decimal> variable);
        bool Remove(string name);
        void Clear();

        bool TryGet(string name, out Variable<decimal> variable);
    }
}

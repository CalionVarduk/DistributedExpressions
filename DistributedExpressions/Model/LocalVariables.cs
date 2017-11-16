using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;

namespace DistributedExpressions.Model
{
    public class LocalVariables : ILocalVariables
    {
        public static bool IsValidName(string name)
        {
            if (name.Length > 0 && name.Length <= 16 && !char.IsDigit(name[0]))
            {
                for (int i = 0; i < name.Length; ++i)
                    if (!char.IsLetterOrDigit(name[i]) && name[i] != '_') return false;
                return true;
            }
            return false;
        }

        private VariableCollection<decimal> _variables;

        public decimal this [string name] { get { return _variables[name]; } }
        public int Count { get { return _variables.Count; } }

        public LocalVariables()
        {
            _variables = new VariableCollection<decimal>();
        }

        public bool Exists(string name)
        {
            return _variables.Exists(name);
        }

        public bool Set(Variable<decimal> variable)
        {
            if (!IsValidName(variable.Name)) return false;
            _variables.Add(variable);
            return true;
        }

        public bool Remove(string name)
        {
            return _variables.Remove(name);
        }

        public void Clear()
        {
            _variables.Clear();
        }

        public bool TryGet(string name, out Variable<decimal> variable)
        {
            return _variables.TryGet(name, out variable); 
        }
    
        public IEnumerator<Variable<decimal>> GetEnumerator()
        {
            return new Enumerator(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
 	        return GetEnumerator();
        }

        public class Enumerator : IEnumerator<Variable<decimal>>
        {
            private IEnumerator<KeyValuePair<string, decimal>> _enumerator;

            public Variable<decimal> Current
            {
                get
                {
                    var kvp = _enumerator.Current;
                    return new Variable<decimal>(kvp.Key, kvp.Value);
                }
            }

            object System.Collections.IEnumerator.Current { get { return Current; } }

            public Enumerator(LocalVariables variables)
            {
                _enumerator = variables._variables.GetEnumerator();
            }

            public bool MoveNext()
            {
                return _enumerator.MoveNext();
            }

            public void Reset()
            {
                _enumerator.Reset();
            }

            public void Dispose()
            {
                _enumerator.Dispose();
            }
        }
    }
}

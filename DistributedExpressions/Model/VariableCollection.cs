using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedExpressions.Model
{
    public class VariableCollection<T> : IEnumerable<KeyValuePair<string, T>>
    {
        private Dictionary<string, T> _vars;

        public int Count { get { return _vars.Count; } }

        public T this[string name]
        {
            get { return _vars[name]; }
            set { _vars[name] = value; }
        }

        public string[] Names { get { return _vars.Keys.ToArray<string>(); } }

        public Variable<T>[] Variables
        {
            get
            {
                int i = 0;
                var variables = new Variable<T>[_vars.Count];

                foreach (var kvp in _vars)
                    variables[i++] = new Variable<T>(kvp.Key, kvp.Value);

                return variables;
            }
        }

        public VariableCollection()
        {
            _vars = new Dictionary<string, T>();
        }

        public bool Exists(string name)
        {
            return _vars.ContainsKey(name);
        }

        public bool Add(string name, T value)
        {
            if (Exists(name))
            {
                _vars[name] = value;
                return false;
            }
            _vars.Add(name, value);
            return true;
        }

        public bool Add(Variable<T> variable)
        {
            return Add(variable.Name, variable.Value);
        }

        public bool Remove(string name)
        {
            return _vars.Remove(name);
        }

        public void Clear()
        {
            _vars.Clear();
        }

        public bool TryGet(string name, out Variable<T> variable)
        {
            T t;
            if (!_vars.TryGetValue(name, out t))
            {
                variable = null;
                return false;
            }
            variable = new Variable<T>(name, t);
            return true;
        }

        public IEnumerator<KeyValuePair<string, T>> GetEnumerator()
        {
            return _vars.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

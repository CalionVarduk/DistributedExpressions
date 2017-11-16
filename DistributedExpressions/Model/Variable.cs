using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedExpressions.Model
{
    public class Variable<T>
    {
        private string _name;
        public string Name { get { return _name; } set { _name = value == null ? "" : value; } }

        public bool IsUnnamed { get { return _name.Length == 0; } }
        public T Value { get; set; }

        public Variable() : this("", default(T))
        { }

        public Variable(string name) : this(name, default(T))
        { }

        public Variable(string name, T value)
        {
            Name = name;
            Value = value;
        }

        public override int GetHashCode()
        {
            return _name.GetHashCode();
        }
    }
}

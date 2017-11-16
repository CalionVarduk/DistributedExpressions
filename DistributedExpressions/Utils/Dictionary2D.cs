using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedExpressions.Utils
{
    public class Dictionary2D<TKey, TValue> : IDictionary<TKey, List<TValue>>
    {
        private Dictionary<TKey, List<TValue>> _map;

        public List<TValue> this[TKey key]
        {
            get { return _map[key]; }
            set
            {
                if (value == null) throw new ArgumentNullException();
                _map[key] = value;
            }
        }

        public TValue this[TKey key, int index]
        {
            get { return _map[key][index]; }
            set { _map[key][index] = value; }
        }

        public int Count { get { return _map.Count; } }
        public ICollection<TKey> Keys { get { return _map.Keys; } }
        public ICollection<List<TValue>> Values { get { return _map.Values; } }

        public Dictionary2D()
        {
            _map = new Dictionary<TKey, List<TValue>>();
        }

        public int CountAll()
        {
            int count = 0;
            foreach (var i in _map)
                count += i.Value.Count;
            return count;
        }

        public int CountAt(TKey key)
        {
            List<TValue> v;
            return _map.TryGetValue(key, out v) ? v.Count : 0;
        }

        public bool AddKey(TKey key)
        {
            if (ContainsKey(key)) return false;
            _map[key] = new List<TValue>();
            return true;
        }

        public bool ContainsKey(TKey key)
        {
            return _map.ContainsKey(key);
        }

        public bool ContainsValue(TKey key, TValue value)
        {
            List<TValue> v;
            if (!_map.TryGetValue(key, out v)) return false;
            return v.Contains(value);
        }

        public bool ContainsValueAt(TKey key, int index, TValue value)
        {
            List<TValue> v;
            if (!_map.TryGetValue(key, out v)) return false;
            return v[index].Equals(value);
        }

        public void Add(TKey key, List<TValue> value)
        {
            List<TValue> v;
            if (!_map.TryGetValue(key, out v))
            {
                v = new List<TValue>();
                _map[key] = v;
            }
            v.AddRange(value);
        }

        public void Add(TKey key, TValue value)
        {
            List<TValue> v;
            if (!_map.TryGetValue(key, out v))
            {
                v = new List<TValue>();
                _map[key] = v;
            }
            v.Add(value);
        }

        public bool Remove(TKey key)
        {
            return _map.Remove(key);
        }

        public bool Remove(TKey key, TValue value)
        {
            List<TValue> v;
            if (!_map.TryGetValue(key, out v)) return false;
            return v.Remove(value);
        }

        public bool RemoveAt(TKey key, int index)
        {
            List<TValue> v;
            if (!_map.TryGetValue(key, out v)) return false;
            v.RemoveAt(index);
            return true;
        }

        public bool TryGetValue(TKey key, out List<TValue> list)
        {
            return _map.TryGetValue(key, out list);
        }

        public bool TryGetValueAt(TKey key, int index, out TValue value)
        {
            List<TValue> v;
            if (!_map.TryGetValue(key, out v) || index < 0 || index >= v.Count)
            {
                value = default(TValue);
                return false;
            }
            value = v[index];
            return true;
        }

        public void Clear()
        {
            _map.Clear();
        }

        public bool ClearAt(TKey key)
        {
            List<TValue> v;
            if (!_map.TryGetValue(key, out v)) return false;
            v.Clear();
            return true;
        }

        public IEnumerator<KeyValuePair<TKey, List<TValue>>> GetEnumerator()
        {
            return _map.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void System.Collections.Generic.ICollection<KeyValuePair<TKey, List<TValue>>>.Add(KeyValuePair<TKey, List<TValue>> item)
        {
            Add(item.Key, item.Value);
        }

        bool System.Collections.Generic.ICollection<KeyValuePair<TKey, List<TValue>>>.Contains(KeyValuePair<TKey, List<TValue>> item)
        {
            return ContainsKey(item.Key);
        }

        void System.Collections.Generic.ICollection<KeyValuePair<TKey, List<TValue>>>.CopyTo(KeyValuePair<TKey, List<TValue>>[] array, int arrayIndex)
        {
            (_map as ICollection<KeyValuePair<TKey, List<TValue>>>).CopyTo(array, arrayIndex);
        }

        bool System.Collections.Generic.ICollection<KeyValuePair<TKey, List<TValue>>>.IsReadOnly
        {
            get { return (_map as ICollection<KeyValuePair<TKey, List<TValue>>>).IsReadOnly; }
        }

        bool System.Collections.Generic.ICollection<KeyValuePair<TKey, List<TValue>>>.Remove(KeyValuePair<TKey, List<TValue>> item)
        {
            return Remove(item.Key);
        }
    }
}

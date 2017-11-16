using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedExpressions.Utils
{
    public class Codebook<T> : IEnumerable<T>
    {
        private Dictionary<T, int> _codes;
        private List<T> _items;

        public int Count { get { return _codes.Count; } }
        public IReadOnlyList<T> Items { get { return _items; } }

        public T this[int code]
        {
            get { return GetItem(code); }
            set { SetItem(code, value); }
        }

        public Codebook()
        {
            _codes = new Dictionary<T, int>();
            _items = new List<T>();
        }

        public bool Contains(T item)
        {
            return _codes.ContainsKey(item);
        }

        public int GetCode(T item)
        {
            int i;
            if (!_codes.TryGetValue(item, out i)) return -1;
            return i;
        }

        public T GetItem(int code)
        {
            return _items[code];
        }

        public bool Add(T item)
        {
            if (Contains(item)) return false;
            _items.Add(item);
            _codes.Add(item, Count);
            return true;
        }

        public int AddRange(IEnumerable<T> range)
        {
            int count = 0;
            if (range != null)
                foreach (var it in range)
                    if (Add(it)) ++count;

            return count;
        }

        public void SetItem(int code, T item)
        {
            T t = _items[code];
            _codes.Add(item, code);
            _codes.Remove(t);
            _items[code] = item;
        }

        public bool Remove(T item)
        {
            int code = GetCode(item);
            if (code == -1) return false;
            _codes.Remove(item);

            for (int i = code; i < Count; ++i)
            {
                _items[i] = _items[i + 1];
                _codes[_items[i]] = i;
            }
            _items.RemoveAt(Count);
            return true;
        }

        public void RemoveAt(int code)
        {
            _codes.Remove(_items[code]);

            for (int i = code; i < Count; ++i)
            {
                _items[i] = _items[i + 1];
                _codes[_items[i]] = i;
            }
            _items.RemoveAt(Count);
        }

        public int RemoveRange(IEnumerable<T> range)
        {
            List<int> indices = new List<int>();
            foreach (var it in range)
            {
                int v;
                if (_codes.TryGetValue(it, out v))
                {
                    indices.Add(v);
                    _codes.Remove(it);
                }
            }
            if (indices.Count == 0) return 0;

            indices.Sort();
            indices.Add(_items.Count);

            for (int i = 1; i < indices.Count; ++i)
            {
                int end = indices[i] - i;
                for (int j = indices[i - 1] - i + 1; j < end; ++j)
                {
                    _items[j] = _items[j + i];
                    _codes[_items[j]] = j;
                }
            }
            int len = indices.Count - 1;
            _items.RemoveRange(_items.Count - len, len);
            return len;
        }

        public int RemoveRangeAt(int code, int length)
        {
            if (length > 0)
            {
                int end = code + length;
                if (end > Count)
                {
                    end = Count;
                    length = end - code;
                    if (length < 0) return 0;
                }

                for (int i = code; i < end; ++i)
                    _codes.Remove(_items[i]);

                for (int i = code; i < Count; ++i)
                {
                    _items[i] = _items[i + length];
                    _codes[_items[i]] = i;
                }
                _items.RemoveRange(_items.Count - length, length);
                return length;
            }
            return 0;
        }

        public bool TryGetCodeOf(T item, out int code)
        {
            return _codes.TryGetValue(item, out code);
        }

        public void Clear()
        {
            _codes.Clear();
            _items.Clear();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

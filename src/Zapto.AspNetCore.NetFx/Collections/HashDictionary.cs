using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Zapto.AspNetCore.Collections;

internal class HashDictionary : IDictionary<object, object?>
{
    private IDictionary _dictionary = null!;
    private readonly ObjectCollection _keys = new();
    private readonly ObjectCollection _values = new();

    public void SetDictionary(IDictionary dictionary)
    {
        _dictionary = dictionary;
        _keys.SetCollection(dictionary.Keys);
        _values.SetCollection(dictionary.Values);
    }

    public void Reset()
    {
        _dictionary = null!;
        _keys.Reset();
        _values.Reset();
    }

    public IEnumerator<KeyValuePair<object, object?>> GetEnumerator()
    {
        return _dictionary
            .Cast<DictionaryEntry>()
            .Select(entry => new KeyValuePair<object, object?>(entry.Key, entry.Value))
            .GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_dictionary).GetEnumerator();
    }

    public void Add(KeyValuePair<object, object?> item)
    {
        _dictionary.Add(item.Key, item.Value);
    }

    public void Clear()
    {
        _dictionary.Clear();
    }

    public bool Contains(KeyValuePair<object, object?> item)
    {
        return _dictionary.Contains(item);
    }

    public void CopyTo(KeyValuePair<object, object?>[] array, int arrayIndex)
    {
        _dictionary.CopyTo(array, arrayIndex);
    }

    public bool Remove(KeyValuePair<object, object?> item)
    {
        if (!_dictionary.Contains(item.Key) || _dictionary[item.Key] != item.Value)
        {
            return false;
        }

        _dictionary.Remove(item.Key);
        return true;
    }

    public int Count => _dictionary.Count;

    public bool IsReadOnly => _dictionary.IsReadOnly;

    public bool ContainsKey(object key)
    {
        return _dictionary.Contains(key);
    }

    public void Add(object key, object? value)
    {
        _dictionary.Add(key, value);
    }

    public bool Remove(object key)
    {
        _dictionary.Remove(key);
        return true;
    }

    public bool TryGetValue(object key, out object? value)
    {
        if (_dictionary.Contains(key))
        {
            value = _dictionary[key];
            return true;
        }

        value = default;
        return false;
    }

    public object? this[object key]
    {
        get => _dictionary[key];
        set => _dictionary[key] = value;
    }

    public ICollection<object> Keys => _keys;

    public ICollection<object?> Values => _values!;

    private class ObjectCollection : ICollection<object>
    {
        private ICollection _collection = null!;

        public void SetCollection(ICollection collection)
        {
            _collection = collection;
        }

        public void Reset()
        {
            _collection = null!;
        }

        IEnumerator<object> IEnumerable<object>.GetEnumerator()
        {
            return _collection.Cast<object>().GetEnumerator();
        }

        public IEnumerator GetEnumerator()
        {
            return _collection.GetEnumerator();
        }

        public void Add(object item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(object item)
        {
            foreach (var obj in _collection)
            {
                if (obj == item)
                {
                    return true;
                }
            }

            return false;
        }

        public void CopyTo(object[] array, int arrayIndex)
        {
            _collection.CopyTo(array, arrayIndex);
        }

        public bool Remove(object item)
        {
            throw new NotSupportedException();
        }

        public int Count => _collection.Count;

        public bool IsReadOnly => true;
    }
}

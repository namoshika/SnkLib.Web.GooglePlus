using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace SunokoLibrary.Collection.Generic
{
    public class CacheDictionary<TKey, TCache, TValue> : IEnumerable<KeyValuePair<TKey, TCache>>
        where TCache : ICacheInfo<TValue>
    {
        public CacheDictionary(int cacheLength, int cacheArrayLength, Func<TValue, TCache> cacheWrapGenerator)
        {
            _cacheWrapGenerator = cacheWrapGenerator;
            _cacheLength = cacheLength;
            _caches = Enumerable.Range(0,cacheArrayLength).Select(idx => new Dictionary<TKey, TCache>()).ToArray();
            _currentCacheIndex = 0;

            //TValue同士の加算を行う式を生成
            var paramExpA = Expression.Parameter(typeof(TValue), "value1");
            var paramExpB = Expression.Parameter(typeof(TValue), "value2");
            _addFunc = (Func<TValue, TValue, TValue>)Expression.Lambda(
                Expression.Add(paramExpA, paramExpB), paramExpA, paramExpB).Compile();
            _notEqualFunc = (Func<TValue, TValue, bool>)Expression.Lambda(
                Expression.NotEqual(paramExpA, paramExpB), paramExpA, paramExpB).Compile();
        }
        int _cacheLength;
        int _currentCacheIndex;
        Dictionary<TKey, TCache>[] _caches;
        Func<TValue, TValue, TValue> _addFunc;
        Func<TValue, TValue, bool> _notEqualFunc;
        Func<TValue, TCache> _cacheWrapGenerator;

        public TCache this[TKey key]
        {
            get
            {
                TCache val;
                if (TryGetValue(key, out val) == false)
                    throw new KeyNotFoundException("引数keyを持つ要素が存在しませんでした。");
                return val;
            }
        }
        public TCache Update(TKey key, TValue updateData)
        {
            TCache result;
            lock (_caches)
                if (TryGetValue(key, out result))
                {
                    if (_notEqualFunc(result.Value, updateData))
                        (result = _caches[_currentCacheIndex][key]).Value = _addFunc(result.Value, updateData);
                }
                else
                    if (updateData != null)
                        result = Add(key, updateData);

            return result;
        }
        public TCache Add(TKey key, TValue value)
        {
            lock (_caches)
            {
                if (_caches[_currentCacheIndex].Count >= _cacheLength)
                {
                    _currentCacheIndex = (_currentCacheIndex > 0 ? _currentCacheIndex : _caches.Length) - 1;
                    _caches[_currentCacheIndex].Clear();
                }
                var wrap = _cacheWrapGenerator(value);
                _caches[_currentCacheIndex].Add(key, wrap);
                return wrap;
            }
        }
        public bool Remove(TKey key)
        {
            lock (_caches)
            {
                var isFound = false;
                var i = 0;
                do
                {
                    isFound = _caches[(_currentCacheIndex + i) % _caches.Length].Remove(key);
                    if (isFound)
                        break;
                    i++;
                }
                while (i < _caches.Length);
                return isFound;
            }
        }
        public bool ContainsKey(TKey key)
        {
            lock (_caches)
            {
                var isFound = false;
                var i = 0;
                do
                {
                    isFound = _caches[(_currentCacheIndex + i) % _caches.Length].ContainsKey(key);
                    if (isFound)
                        break;
                    i++;
                }
                while (i < _caches.Length);
                return isFound;
            }
        }
        public bool TryGetValue(TKey key, out TCache value)
        {
            lock (_caches)
            {
                var isFound = false;
                var i = 0;
                do
                {
                    isFound = _caches[(_currentCacheIndex + i) % _caches.Length].TryGetValue(key, out value);
                    if (isFound)
                    {
                        if (i > 0)
                        {
                            _caches[(_currentCacheIndex + i) % _caches.Length].Remove(key);
                            if (_caches[_currentCacheIndex].Count < _cacheLength)
                                _caches[_currentCacheIndex].Add(key, value);
                            else
                            {
                                _currentCacheIndex = (_currentCacheIndex > 0 ? _currentCacheIndex : _caches.Length) - 1;
                                _caches[_currentCacheIndex].Clear();
                                _caches[_currentCacheIndex].Add(key, value);
                            }
                        }
                        break;
                    }
                    i++;
                }
                while (i < _caches.Length);

                value = isFound ? value : default(TCache);
                return isFound;
            }
        }
        public IEnumerator<KeyValuePair<TKey, TCache>> GetEnumerator()
        { return _caches.SelectMany(dict => dict).GetEnumerator(); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        { return GetEnumerator(); }

        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.RootHidden)]
        IEnumerable<KeyValuePair<TKey, TCache>> Debug_DebuggerDisplay
        { get { return _caches.SelectMany(dict => dict); } }
    }
    public interface ICacheInfo<T> { T Value { get; set; } }
}
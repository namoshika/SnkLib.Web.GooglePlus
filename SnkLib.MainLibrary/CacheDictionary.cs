using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace SunokoLibrary.Collection.Generic
{
    public class CacheDictionary<TKey, TCache, TValue> : ICacheDictionary<TKey, TCache, TValue>
        where TCache : ICacheInfo<TValue>
    {
        public CacheDictionary(int cacheSize, int cacheOutSize, Func<TValue, TCache> cacheWrapGenerator)
        {
            _cacheWrapGenerator = cacheWrapGenerator;
            _cacheOutSize = cacheOutSize;
            _cacheSize = cacheSize;

            //TValue同士の加算を行う式を生成
            var paramExpA = Expression.Parameter(typeof(TValue), "value1");
            var paramExpB = Expression.Parameter(typeof(TValue), "value2");
            _addFunc = (Func<TValue, TValue, TValue>)Expression.Lambda(
                Expression.Add(paramExpA, paramExpB), paramExpA, paramExpB).Compile();
            _notEqualFunc = (Func<TValue, TValue, bool>)Expression.Lambda(
                Expression.NotEqual(paramExpA, paramExpB), paramExpA, paramExpB).Compile();
        }

        int _cacheSize, _cacheOutSize;
        Dictionary<TKey, TCache> _values = new Dictionary<TKey, TCache>();
        LinkedList<TKey> _usedKeyLog = new LinkedList<TKey>();
        Dictionary<TKey, LinkedListNode<TKey>> _keyNodeDict = new Dictionary<TKey, LinkedListNode<TKey>>();
        Func<TValue, TValue, TValue> _addFunc;
        Func<TValue, TValue, bool> _notEqualFunc;
        Func<TValue, TCache> _cacheWrapGenerator;

        public TCache Update(TKey key, TValue newValue)
        {
            TCache result;
            lock(_values)
                if (_values.ContainsKey(key))
                {
                    if (_notEqualFunc(_values[key].Value, newValue) == false)
                        return _values[key];
                    (result = _values[key]).Value = _addFunc(_values[key].Value, newValue);

                    var node = _keyNodeDict[key];
                    _usedKeyLog.Remove(node);
                    _usedKeyLog.AddFirst(node);
                }
                else
                {
                    result = _cacheWrapGenerator(newValue);
                    _values.Add(key, result);

                    var newNode = new LinkedListNode<TKey>(key);
                    _usedKeyLog.AddFirst(newNode);
                    _keyNodeDict.Add(key, newNode);

                    if (_usedKeyLog.Count > _cacheSize)
                        for (var i = 0; i < _cacheOutSize; i++)
                        {
                            _keyNodeDict.Remove(_usedKeyLog.Last.Value);
                            _values.Remove(_usedKeyLog.Last.Value);
                            _usedKeyLog.RemoveLast();
                        }
                }
            return result;
        }
        public TCache Update(TKey key, Func<TValue> substituteValueGenerator)
        {
            lock (_values)
                return _values.ContainsKey(key) ? Get(key) : Update(key, substituteValueGenerator());
        }
        public TCache Get(TKey key)
        {
            lock (_values)
                if (_values.ContainsKey(key))
                {
                    var node = _keyNodeDict[key];
                    _usedKeyLog.Remove(node);
                    _usedKeyLog.AddFirst(node);
                    return _values[key];
                }
                else
                    throw new KeyNotFoundException();
        }
    }
    public interface ICacheDictionary<TKey, TCache, TValue> where TCache : ICacheInfo<TValue>
    {
        TCache Update(TKey key, TValue newValue);
        TCache Update(TKey key, Func<TValue> substituteValueGenerator);
        TCache Get(TKey key);
    }
    public interface ICacheInfo<T> { T Value { get; set; } }
}
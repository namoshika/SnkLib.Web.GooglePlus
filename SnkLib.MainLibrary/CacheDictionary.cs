using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace SunokoLibrary.Collections.Generic
{
    public class CacheDictionary<TKey, TCache, TValue> : ICacheDictionary<TKey, TCache, TValue>
        where TCache : ICacheInfo<TValue>
    {
        public CacheDictionary(int cacheSize, int cacheOutSize, bool isAddMode, Func<TValue, TCache> cacheWrapGenerator)
        {
            _cacheWrapGenerator = cacheWrapGenerator;
            _cacheOutSize = cacheOutSize;
            _cacheSize = cacheSize;
            _isAddMode = isAddMode;

            //TValue同士の加算を行う式を生成
            var paramExpA = Expression.Parameter(typeof(TValue), "value1");
            var paramExpB = Expression.Parameter(typeof(TValue), "value2");
            if (isAddMode)
                _addFunc = (Func<TValue, TValue, TValue>)Expression.Lambda(
                    Expression.Add(paramExpA, paramExpB), paramExpA, paramExpB).Compile();
            _notEqualFunc = (Func<TValue, TValue, bool>)Expression.Lambda(
                Expression.NotEqual(paramExpA, paramExpB), paramExpA, paramExpB).Compile();
        }
        object _owner;
        bool _isAddMode;
        int _cacheSize, _cacheOutSize;
        Dictionary<TKey, TCache> _values = new Dictionary<TKey, TCache>();
        LinkedList<TKey> _usedKeyLog = new LinkedList<TKey>();
        Dictionary<TKey, LinkedListNode<TKey>> _keyNodeDict = new Dictionary<TKey, LinkedListNode<TKey>>();
        Func<TValue, TValue, TValue> _addFunc;
        Func<TValue, TValue, bool> _notEqualFunc;
        Func<TValue, TCache> _cacheWrapGenerator;

        public void SetOwner(object owner)
        { _owner = owner; }
        public TCache Update(object caller, TKey key, Func<TValue> substituteValueGenerator)
        {
            lock (_values)
                return _values.ContainsKey(key) ? Get(caller, key) : Update(caller, key, substituteValueGenerator());
        }
        public virtual TCache Update(object caller, TKey key, TValue newValue)
        {
            if (_owner != null && caller != _owner)
                throw new ArgumentException("引数のcallerがコンストラクタで指定したownerと異なります。オブジェクトの使用者と所有者は統一してください。");

            TCache result;
            lock(_values)
                if (_values.ContainsKey(key))
                {
                    if (_notEqualFunc(_values[key].Value, newValue) == false)
                        return _values[key];
                    if (_isAddMode)
                        (result = _values[key]).Value =
                            _addFunc(_values[key].Value, newValue);
                    else
                        (result = _values[key]).Value =
                            _notEqualFunc(newValue, default(TValue)) ? newValue : _values[key].Value;

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
                            var obj = _values[_usedKeyLog.Last.Value];
                            _keyNodeDict.Remove(_usedKeyLog.Last.Value);
                            _values.Remove(_usedKeyLog.Last.Value);
                            _usedKeyLog.RemoveLast();
                            OnCacheOuted(new CacheoutEventArgs<TValue>(obj.Value));
                        }
                }
            return result;
        }
        public virtual TCache Get(object caller, TKey key)
        {
            if (_owner != null && caller != _owner)
                throw new ArgumentException("引数のcallerがコンストラクタで指定したownerと異なります。オブジェクトの使用者と所有者は統一してください。");
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

        public event EventHandler<CacheoutEventArgs<TValue>> CacheOuted;
        protected virtual void OnCacheOuted(CacheoutEventArgs<TValue> e)
        {
            if (CacheOuted != null)
                CacheOuted(this, e);
        }
    }
    public interface ICacheDictionary<TKey, TCache, TValue> where TCache : ICacheInfo<TValue>
    {
        void SetOwner(object owner);
        TCache Update(object caller, TKey key, TValue newValue);
        TCache Update(object caller, TKey key, Func<TValue> substituteValueGenerator);
        TCache Get(object caller, TKey key);
    }
    public interface ICacheInfo<T> { T Value { get; set; } }

    public class CacheoutEventArgs<T>:EventArgs
    {
        public CacheoutEventArgs(T value) { Value = value; }
        public T Value { get; private set; }
    }
}
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus
{
    using SunokoLibrary.Collection.Generic;
    using SunokoLibrary.Threading;
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class ActivityContainer : AccessorBase, IDisposable
    {
        public ActivityContainer(PlatformClient client)
            : base(client) { BeganTimeToBind = DateTime.MaxValue; }

        readonly object _syncerStream = new object();
        readonly ICacheDictionary<string, ActivityCache, ActivityData> _activityCache =
            new CacheDictionary<string, ActivityCache, ActivityData>(1200, 400, true, dt => new ActivityCache() { Value = dt });
        readonly Subject<object> _streamObserver = new Subject<object>();
        bool _isConnected;
        int _streamSessionRefCount;
        IDisposable _streamSession;
        IConnectableObservable<object> _stream;

        public DateTime BeganTimeToBind { get; private set; }
        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                var isChanged = _isConnected != value;
                _isConnected = value;
                if (isChanged)
                    OnChangedIsConnected(new EventArgs());
            }
        }

        public IObservable<object> GetStream()
        {
            return Observable
                .If(() => Client.IsLoadedHomeInitData, Observable.Return(0), Observable.Throw(
                    new InvalidOperationException("PlatformClient.IsLoadedHomeInitDataがfalseの状態で使うことはできません。"), 0))
                .SelectMany(hoge => Observable.Create<object>(subject =>
                    {
                        lock (_syncerStream)
                        {
                            if (_stream == null)
                            {
                                _stream = Client.ServiceApi.GetStreamAttacher(Client)
                                    .Merge(_streamObserver).Select(ConvertDataToInfo).Publish();
                                _streamSession = _stream.Connect();
                                BeganTimeToBind = DateTime.UtcNow;
                                IsConnected = true;
                            }
                            _streamSessionRefCount++;
                            var strm = _stream.Subscribe(subject.OnNext,
                                ex =>
                                    {
                                        lock (_syncerStream)
                                        {
                                            _stream = null;
                                            _streamSessionRefCount = 0;
                                            _streamSession.Dispose();
                                            BeganTimeToBind = DateTime.MaxValue;
                                            IsConnected = false;
                                        }
                                        try { subject.OnError(ex); }
                                        catch { }
                                    }, subject.OnCompleted);
                            var disposer = (Action)(() =>
                                {
                                    strm.Dispose();
                                    lock (_syncerStream)
                                        if (_streamSessionRefCount > 0)
                                        {
                                            _streamSessionRefCount = Math.Max(_streamSessionRefCount - 1, 0);
                                            if (_streamSessionRefCount == 0)
                                            {
                                                _stream = null;
                                                _streamSession.Dispose();
                                                BeganTimeToBind = DateTime.MaxValue;
                                                IsConnected = false;
                                            }
                                        }
                                });
                            return disposer;
                        }
                    })
                );
        }
        public ActivityInfo GetActivityInfo(string targetId)
        {
            var cache = InternalGetActivityCache(targetId);
            return new ActivityInfo(Client, cache.Value);
        }
        public void Dispose()
        {
            lock (_syncerStream)
                if (_streamSession != null)
                    _streamSession.Dispose();
        }
        internal void InternalSendObjectToStream(object item)
        {
            lock (_syncerStream)
                if (_streamObserver != null)
                    _streamObserver.OnNext(item);
        }
        internal ActivityCache InternalGetActivityCache(string targetId)
        { return _activityCache.Update(targetId, () => new ActivityData(targetId)); }
        internal ActivityData InternalUpdateActivity(ActivityData newValue)
        { return _activityCache.Update(newValue.Id, newValue).Value; }
        internal ActivityInfo InternalGetAndUpdateActivity(ActivityData newValue)
        { return new ActivityInfo(Client, InternalUpdateActivity(newValue)); }
        object ConvertDataToInfo(object item)
        {
            if (item is ActivityData)
                return Client.Activity.InternalGetAndUpdateActivity((ActivityData)item);
            else if (item is ProfileData)
                return Client.People.InternalGetAndUpdateProfile((ProfileData)item);
            else if (item is CommentData)
            {
                var cData = (CommentData)item;
                var aData = Client.Activity.InternalGetActivityCache(cData.ActivityId).Value;
                var info = new CommentInfo(Client, cData, aData);
                if (cData.Status == PostStatusType.Removed)
                {
                    aData.Comments = aData.Comments != null ? aData.Comments
                        .Where(dt => dt.CommentId != cData.CommentId).ToArray() : null;
                    if(aData.CommentLength.HasValue)
                        aData.CommentLength--;
                }
                else
                    if (aData.Comments != null)
                    {
                        var i = 0;
                        var cDatas = aData.Comments.ToArray();
                        var found = false;
                        for (; i < cDatas.Length; i++)
                            if (cDatas[i].CommentId == cData.CommentId)
                            {
                                cDatas[i] = cData;
                                found = true;
                                break;
                            }
                        if (found == false)
                        {
                            cDatas = cDatas.Concat(new[] { cData }).ToArray();
                            if(aData.CommentLength.HasValue)
                                aData.CommentLength++;
                        }
                        aData.Comments = cDatas;
                    }
                    else
                    {
                        aData.Comments = new[] { cData };
                        if (aData.CommentLength.HasValue)
                            aData.CommentLength++;
                    }
                return info;
            }
            else
                return item;
        }

        public event EventHandler ChangedIsConnected;
        protected virtual void OnChangedIsConnected(EventArgs e)
        {
            if (ChangedIsConnected != null)
                ChangedIsConnected(this, e);
        }
    }
    public class ActivityCache : ICacheInfo<ActivityData>
    {
        public ActivityData Value { get; set; }
        public readonly AsyncLocker SyncerUpdateActivity = new AsyncLocker();
    }
}

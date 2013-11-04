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

    public class ActivityContainer : AccessorBase
    {
        public ActivityContainer(PlatformClient client)
            : base(client) { BeganTimeToBind = DateTime.MaxValue; }

        readonly object _syncerStream = new object();
        readonly CacheDictionary<string, ActivityCache, ActivityData> _activityCache =
            new CacheDictionary<string, ActivityCache, ActivityData>(600, 3, dt => new ActivityCache() { Value = dt });
        int _streamSessionRefCount;
        IDisposable _streamSession;
        IConnectableObservable<object> _stream;

        public DateTime BeganTimeToBind { get; private set; }
        public bool IsConnected { get { return BeganTimeToBind != DateTime.MaxValue; } }

        public IObservable<object> GetStream()
        {
            return Observable
                .Defer(() => Client.IsLoadedHomeInitData
                    ? Observable.Return(0) 
                    : Observable.Throw(new InvalidOperationException(
                        "PlatformClient.IsLoadedHomeInitDataがfalseの状態で使うことはできません。"), 0))
                .SelectMany(hoge => Observable.Create<object>(obsrvr =>
                    {
                        lock (_syncerStream)
                            if (_stream == null)
                            {
                                //_gottenTalkGadgetCookieでPlatformClient生成後の初回の実行か
                                //どうかを確認。初回の場合は認証処理をかませておく
                                //if(Client.Cookies.GetCookies("talkgadget.com
                                _stream = Client.ServiceApi.GetStreamAttacher(Client).Publish();
                                _streamSession = _stream.Connect();
                                _streamSessionRefCount++;
                                BeganTimeToBind = DateTime.UtcNow;
                                OnChangedIsConnected(new EventArgs());
                            }
                        var strm = _stream.Subscribe(obsrvr);
                        var disposer = (Action)(() =>
                        {
                            strm.Dispose();
                            lock (_syncerStream)
                                if (_streamSessionRefCount == 0)
                                {
                                    _stream = null;
                                    _streamSession.Dispose();
                                    _streamSessionRefCount--;
                                    BeganTimeToBind = DateTime.MaxValue;
                                    OnChangedIsConnected(new EventArgs());
                                }
                        });
                        return disposer;
                    })
                )
            .Select(item =>
                {
                    if (item is ActivityData)
                        return Client.Activity.InternalGetAndUpdateActivity((ActivityData)item);
                    else if (item is ProfileData)
                        return Client.Relation.InternalGetAndUpdateProfile((ProfileData)item);
                    else if (item is CommentData)
                    {
                        var cData = (CommentData)item;
                        var aData = Client.Activity.InternalGetActivityCache(cData.ActivityId).Value;
                        var info = new CommentInfo(Client, cData, aData);
                        if (cData.Status == PostStatusType.Removed)
                            aData.Comments = aData.Comments != null ? aData.Comments
                                .Where(dt => dt.CommentId != cData.CommentId).ToArray() : null;
                        else
                        {
                            var cDataAry = new[] { cData };
                            aData.Comments = aData.Comments != null ? aData.Comments.Concat(cDataAry).ToArray() : cDataAry;
                        }
                        return info;
                    }
                    else
                        return item;
                });
        }
        public ActivityInfo GetActivityInfo(string id)
        {
            var cache = InternalGetActivityCache(id);
            return new ActivityInfo(Client, cache.Value);
        }
        internal ActivityCache InternalGetActivityCache(string plusId)
        {
            ActivityCache result;
            if (_activityCache.TryGetValue(plusId, out result) == false)
                result = _activityCache.Add(plusId, new ActivityData(id: plusId));
            return result;
        }
        internal ActivityData InternalUpdateActivity(ActivityData newValue)
        { return _activityCache.Update(newValue.Id, newValue).Value; }
        internal ActivityInfo InternalGetAndUpdateActivity(ActivityData newValue)
        { return new ActivityInfo(Client, InternalUpdateActivity(newValue)); }

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

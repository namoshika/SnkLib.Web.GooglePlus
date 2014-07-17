using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus
{
    using SunokoLibrary.Collections.Generic;
    using SunokoLibrary.Threading;
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class PlatformClient : IPlatformClient, IDisposable
    {
        public PlatformClient(Uri plusBaseUrl, Uri talkBaseUrl,
            System.Net.CookieContainer cookie, IApiAccessor accessor,
            ICacheDictionary<string, ProfileCache, ProfileData> profileCacheStorage,
            CacheDictionary<string, ActivityCache, ActivityData> activityCacheStorage)
        {
            var handler = new System.Net.Http.HttpClientHandler() { CookieContainer = cookie };
            Cookies = cookie;
            ServiceApi = accessor;
            PlusBaseUrl = plusBaseUrl;
            TalkBaseUrl = talkBaseUrl;
            NormalHttpClient = new System.Net.Http.HttpClient(handler);
            NormalHttpClient.DefaultRequestHeaders.Add("user-agent", ApiAccessorUtility.UserAgent);
            StreamHttpClient = new System.Net.Http.HttpClient(handler);
            StreamHttpClient.Timeout = TimeSpan.FromMinutes(15);
            StreamHttpClient.DefaultRequestHeaders.Add("user-agent", ApiAccessorUtility.UserAgent);
            People = new PeopleContainer(this, profileCacheStorage);
            Activity = new ActivityContainer(this, activityCacheStorage);
            Notification = new NotificationContainer(this);
        }
        readonly AsyncLocker _updateHomeInitDataLocker = new AsyncLocker();
        InitData _data;

        public bool IsLoadedHomeInitData { get; private set; }
        public Uri PlusBaseUrl { get; private set; }
        public Uri TalkBaseUrl { get; private set; }
        public System.Net.Http.HttpClient NormalHttpClient { get; private set; }
        public System.Net.Http.HttpClient StreamHttpClient { get; private set; }
        public System.Net.CookieContainer Cookies { get; private set; }
        public IApiAccessor ServiceApi { get; private set; }
        public string Afsid
        { get { return AccessorBase.CheckFlag(() => _data.Afsid, () => IsLoadedHomeInitData, () => IsLoadedHomeInitData, "trueでない"); } }
        public string AtValue
        { get { return AccessorBase.CheckFlag(() => _data.AtValue, () => IsLoadedHomeInitData, () => IsLoadedHomeInitData, "trueでない"); } }
        public string BuildLevel
        { get { return AccessorBase.CheckFlag(() => _data.BuildLevel, () => IsLoadedHomeInitData, () => IsLoadedHomeInitData, "trueでない"); } }
        public string EjxValue
        { get { return AccessorBase.CheckFlag(() => _data.EjxValue, () => IsLoadedHomeInitData, () => IsLoadedHomeInitData, "trueでない"); } }
        public string Lang
        { get { return AccessorBase.CheckFlag(() => _data.Lang, () => IsLoadedHomeInitData, () => IsLoadedHomeInitData, "trueでない"); } }
        public string PvtValue
        { get { return AccessorBase.CheckFlag(() => _data.PvtValue, () => IsLoadedHomeInitData, () => IsLoadedHomeInitData, "trueでない"); } }
        public string Tok
        { get { return AccessorBase.CheckFlag(() => _data.Tok, () => IsLoadedHomeInitData, () => IsLoadedHomeInitData, "trueでない"); } }
        public PeopleContainer People { get; private set; }
        public ActivityContainer Activity { get; private set; }
        public NotificationContainer Notification { get; private set; }
        internal ActivityData[] LatestActivities
        { get { return AccessorBase.CheckFlag(() => _data.LatestActivities, () => IsLoadedHomeInitData, () => IsLoadedHomeInitData, "trueでない"); } }
        internal CircleData[] Circles
        { get { return AccessorBase.CheckFlag(() => _data.CircleInfos, () => IsLoadedHomeInitData, () => IsLoadedHomeInitData, "trueでない"); } }

        public Task UpdateHomeInitDataAsync(bool isForced)
        {
            return _updateHomeInitDataLocker.LockAsync(isForced, () => IsLoadedHomeInitData == false, async () =>
                {
                    try
                    {
                        _data = await ServiceApi.GetInitDataAsync(this);
                        IsLoadedHomeInitData = true;
                        return;
                    }
                    catch (Primitive.ApiErrorException e)
                    {
                        throw new FailToOperationException<PlatformClient>(
                            "UpdateHomeInitDataAsync()でjsonの読み込みに失敗。APIへのアクセスに失敗しました。", this, e); ;
                    }
                }, null);
        }
        public void Dispose()
        {
            Activity.Dispose();
            NormalHttpClient.Dispose();
            StreamHttpClient.Dispose();
        }

        static readonly PlatformClientFactory _factory = new PlatformClientFactory();
        public static PlatformClientFactory Factory { get { return _factory; } }
    }
    public class PlatformClientFactory
    {
        public async Task<PlatformClient> Create(System.Net.CookieContainer cookie,
            int accountIndex, IApiAccessor[] accessors = null,
            CacheDictionary<string, ProfileCache, ProfileData> profileCacheStorage = null,
            CacheDictionary<string, ActivityCache, ActivityData> activityCacheStorage = null)
        {
            var accountPrefix = string.Format("u/{0}/", accountIndex);
            accessors = accessors ?? new IApiAccessor[] { new DefaultAccessor() };
            //accessors内で使えるものを検索
            //G+apiバージョンで降順にしたIApiAccessor配列が用いられることを想定してる
            foreach (var item in accessors)
            {
                var client = new PlatformClient(
                    new Uri(PlusBaseUrl, accountPrefix),
                    new Uri(TalkBaseUrl, accountPrefix), cookie, item,
                    profileCacheStorage ?? new CacheDictionary<string, ProfileCache, ProfileData>(1200, 400, true, dt => new ProfileCache() { Value = dt }),
                    activityCacheStorage ?? new CacheDictionary<string, ActivityCache, ActivityData>(1200, 400, true, dt => new ActivityCache() { Value = dt }));
                try
                {
                    await client.UpdateHomeInitDataAsync(true);
                    return client;
                }
                catch (FailToOperationException)
                { client.Dispose(); }
            }
            throw new FailToOperationException("Create()に失敗。使用できるIApiAccessorがありませんでした。", null);
        }

        public static readonly Uri PlusBaseUrl = new Uri("https://plus.google.com/");
        public static readonly Uri TalkBaseUrl = new Uri("https://talkgadget.google.com/");
    }

    public enum NotificationsFilter { All = -1, Mension = 0, PostIntoYou = 1, OtherPost = 2, CircleIn = 3, Game = 4, TaggedImage = 6, }
    public enum AccountBlockType { Ignore, Block, }
    public enum BlockActionType { Remove, Add, }
    public enum SearchTarget { All = 1, Profile = 2, Activity = 3, Sparks = 4, Hangout = 5 }
    public enum SearchRange { Full = 1, YourCircle = 2, Me = 5 }
    public enum ContentType { Album, Image, Link, InteractiveLink, YouTube, Reshare }
}

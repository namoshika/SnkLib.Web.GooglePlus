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
    using SunokoLibrary.Threading;
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class PlatformClient : IPlatformClient, IDisposable
    {
        public PlatformClient(Uri plusBaseUrl, Uri talkBaseUrl, System.Net.CookieContainer cookie, IApiAccessor accessor)
        {
            var handler = new System.Net.Http.HttpClientHandler() { CookieContainer = cookie };
            Cookies = cookie;
            ServiceApi = accessor;
            PlusBaseUrl = plusBaseUrl;
            TalkBaseUrl = talkBaseUrl;
            NormalHttpClient = new System.Net.Http.HttpClient(handler);
            NormalHttpClient.DefaultRequestHeaders.Add(
                "user-agent", "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/30.0.1599.101 Safari/537.36");
            StreamHttpClient = new System.Net.Http.HttpClient(handler);
            StreamHttpClient.Timeout = TimeSpan.FromMinutes(15);
            StreamHttpClient.DefaultRequestHeaders.Add(
                "user-agent", "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/30.0.1599.101 Safari/537.36");
            People = new PeopleContainer(this);
            Activity = new ActivityContainer(this);
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
        public string AtValue
        { get { return AccessorBase.CheckFlag(_data.AtValue, "IsLoadedHomeInitData", () => IsLoadedHomeInitData, "trueでない"); } }
        public string PvtValue
        { get { return AccessorBase.CheckFlag(_data.PvtValue, "IsLoadedHomeInitData", () => IsLoadedHomeInitData, "trueでない"); } }
        public string EjxValue
        { get { return AccessorBase.CheckFlag(_data.EjxValue, "IsLoadedHomeInitData", () => IsLoadedHomeInitData, "trueでない"); } }
        public PeopleContainer People { get; private set; }
        public ActivityContainer Activity { get; private set; }
        public NotificationContainer Notification { get; private set; }
        internal ActivityData[] LatestActivities
        { get { return AccessorBase.CheckFlag(_data.LatestActivities, "IsLoadedHomeInitData", () => IsLoadedHomeInitData, "trueでない"); } }
        internal CircleData[] Circles
        { get { return AccessorBase.CheckFlag(_data.CircleInfos, "IsLoadedHomeInitData", () => IsLoadedHomeInitData, "trueでない"); } }

        public Task UpdateHomeInitDataAsync(bool isForced, TimeSpan? intervalRestriction = null)
        {
            return _updateHomeInitDataLocker.LockAsync(isForced, () => IsLoadedHomeInitData == false, intervalRestriction, async () =>
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
            NormalHttpClient.Dispose();
            StreamHttpClient.Dispose();
        }

        static readonly PlatformClientFactory _factory = new PlatformClientFactory();
        public static PlatformClientFactory Factory { get { return _factory; } }
    }
    public class PlatformClientFactory
    {
        public async Task<PlatformClient> Create(System.Net.CookieContainer cookie, int accountIndex, IApiAccessor accessor = null)
        {
            var accountPrefix = string.Format("u/{0}/", accountIndex);
            var client = new PlatformClient(
                new Uri(PlusBaseUrl, accountPrefix),
                new Uri(TalkBaseUrl, accountPrefix), cookie, accessor ?? new DefaultAccessor());
            try
            {
                await client.UpdateHomeInitDataAsync(true);
                return client;
            }
            catch (FailToOperationException e)
            {
                client.Dispose();
                throw new FailToOperationException("Create()に失敗。ログイン後の初期化処理に失敗しました。", e);
            }
        }

        public static readonly Uri PlusBaseUrl = new Uri("https://plus.google.com/");
        public static readonly Uri TalkBaseUrl = new Uri("https://talkgadget.google.com/");
    }
}

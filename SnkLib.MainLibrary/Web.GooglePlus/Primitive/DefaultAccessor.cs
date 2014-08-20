using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    using SunokoLibrary.Threading;

    public class DefaultAccessor : IApiAccessor, IDataFactoryManager
    {
        public DefaultAccessor(ApiWrapper apiObject = null)
        {
            _apiWrapper = apiObject ?? ApiWrapper.Default;
            _profileFactory = new ProfileDataFactory(this);
            _activityFactory = new ActivityDataFactory(this);
            _commentFactory = new CommentDataFactory(this);
            _attachedFactory = new AttachedDataFactory(this);
            _notificationFactory = new NotificationDataFactory(this);
        }
        readonly ApiWrapper _apiWrapper;
        readonly ProfileDataFactory _profileFactory;
        readonly ActivityDataFactory _activityFactory;
        readonly CommentDataFactory _commentFactory;
        readonly AttachedDataFactory _attachedFactory;
        readonly NotificationDataFactory _notificationFactory;

        public async Task<IPlatformClientBuilder[]> GetAccountListAsync(CookieContainer cookies)
        {
            try
            {
                var client = new System.Net.Http.HttpClient(new System.Net.Http.HttpClientHandler() { CookieContainer = cookies });
                client.DefaultRequestHeaders.Add("user-agent", ApiAccessorUtility.UserAgent);
                var json = JArray.Parse(ApiAccessorUtility.ConvertIntoValidJson(await _apiWrapper.LoadListAccounts(client)));
                var generators = json[1]
                    .Select(item => new PlatformClientBuilder((string)item[3], (string)item[2], ApiAccessorUtility.ConvertReplasableUrl((string)item[4]), (int)item[7], cookies))
                    .ToArray();
                return generators;
            }
            catch (System.Net.Http.HttpRequestException e)
            { throw new FailToOperationException("引数cookiesからログインされているアカウントの取得に失敗しました。", e); }
        }
        [Obsolete("このメソッドを使用すべきではありません。認証を使わずに外部からのCookieの取り込みを検討してください。")]
        public Task<bool> LoginAsync(string email, string password, IPlatformClient client)
        { return _apiWrapper.ConnectToServiceLoginAuth(client.NormalHttpClient, client.PlusBaseUrl, client.Cookies, email, password); }
        public async Task<InitData> GetInitDataAsync(IPlatformClient client)
        {
            var plusPg = await _apiWrapper.LoadHomeInitData(client.NormalHttpClient, client.PlusBaseUrl);

            //apiのロケールやバージョン類を取得
            string buildLabel = null, lang = null, afsid = null;
            var matches = System.Text.RegularExpressions.Regex.Matches(plusPg, "(?<valName>OZ_\\w*) ?= ?(?:'|\")(?<value>[^'\"]+)(?:'|\")");
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                System.Diagnostics.Debug.WriteLineIf(match.Groups["valName"].Value == "OZ_buildLabel", match.Groups["value"].Value);
                switch (match.Groups["valName"].Value)
                {
                    case "OZ_buildLabel":
                        buildLabel = match.Groups["value"].Value;
                        break;
                    case "OZ_lang":
                        lang = match.Groups["value"].Value;
                        break;
                    case "OZ_afsid":
                        afsid = match.Groups["value"].Value;
                        break;
                }
            }
            if (new[] { buildLabel, lang, afsid }.Any(str => str == null))
                throw new ApiErrorException("トップページのパラメータ取得に失敗。ログインセッションが失効しています。", ErrorType.SessionError, new Uri("https://plus.google.com"), null, null, null);

            //api呼び出し用のパラメータ類を取得
            matches = System.Text.RegularExpressions.Regex.Matches(plusPg, "\\((?<json>\\{\\s*key\\s*:(?:\"(?:\\\\\"|[^\"])*\"|[^;])*\\})\\);");
            var hmIntDt = new Dictionary<object, JToken>();
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var txt = match.Groups["json"].Value;
                //移行期のマルチタイプ対策
                if (txt.Trim('{', '}').Split(',').Select(itm => itm.Trim())
                       .Any(itm => itm.IndexOf("data:function(){", 0, Math.Min(itm.Length, 16)) >= 0))
                    continue;
                var json = JToken.Parse(ApiAccessorUtility.ConvertIntoValidJson((txt)));
                hmIntDt.Add(int.Parse((string)json["key"]), json["data"]);
            }
            try
            {
                var atVal = (string)hmIntDt[1][15];
                var pvtVal = (string)hmIntDt[1][28];
                var eJxVal = (string)hmIntDt[161][1][1];
                var circleInfos = hmIntDt[12][0]
                    .Select(item => new CircleData((string)item[0][0], (string)item[1][0], null))
                    .ToArray();
                var latestActivities = hmIntDt[161][1][7]
                    .Where(item => (string)item[0] == "1002")
                    .Select(jsonItem => _activityFactory.Generate(jsonItem[6]["33558957"], ActivityUpdateApiFlag.GetActivities, client))
                    .ToArray();
                return new InitData(atVal, pvtVal, eJxVal, buildLabel, lang, afsid, circleInfos, latestActivities); ;
            }
            catch (KeyNotFoundException e)
            { throw new ApiErrorException("トップページのパラメータ取得に失敗。ログインセッションが失効しています。", ErrorType.SessionError, new Uri("https://plus.google.com"), null, null, e); }
        }
        public async Task<Tuple<CircleData[], ProfileData[]>> GetCircleDatasAsync(IPlatformClient client)
        {
            var lookupedProfiles = new List<ProfileData>();
            var circles = new Dictionary<string, Tuple<string, List<ProfileData>>>();
            var apiResponseTxt = await _apiWrapper.ConnectToLookupCircles(client.NormalHttpClient, client.PlusBaseUrl, client.AtValue);
            var apiResponse = JToken.Parse(ApiAccessorUtility.ConvertIntoValidJson(apiResponseTxt))[0][1];
            var lastUpdateDate = DateTime.UtcNow;

            //サークル一覧生成。List<ProfileInfo>の初期化と一緒にサークル名も
            //この時点で取得してしまう。
            foreach (var item in apiResponse[1])
                circles.Add((string)item[0][0], Tuple.Create((string)item[1][0], new List<ProfileData>()));
            //profileIdをサークルに振り分けてく
            foreach (var item in apiResponse[2])
            {
                var profileId = (string)item[0].ElementAtOrDefault(2) ?? (string)item[0][0];
                var profile = _profileFactory.Generate(item, lastUpdateDate, ProfileUpdateApiFlag.LookupCircle);
                var circleIdLst = new List<string>();
                bool isBlockingId = false;
                foreach (var cidItm in item[3])
                {
                    var cid = (string)cidItm[2][0];
                    circleIdLst.Add(cid);
                    if (cid == "15")
                        isBlockingId = true;
                }
                if (isBlockingId == false)
                    lookupedProfiles.Add(profile);
                foreach (var cid in circleIdLst)
                    //サークルID連想配列にメンバーを追加する
                    circles[cid].Item2.Add(profile);
            }
            //メンバ変数に収めていく。ブロックサークルは別枠扱い
            var resCircles = new List<CircleData>(circles
                .Select(item => new CircleData(item.Key, item.Value.Item1, item.Value.Item2.ToArray()))
                .Concat(new CircleData[]{ new CircleData("anyone", "全員", lookupedProfiles.ToArray()) }));
            //フォローしているプロフィールのリストをつくる
            var circleWithoutBlock = resCircles.Where(dt => dt.Id != "15").ToArray();
            var resProfiles = new List<ProfileData>(apiResponse[2]
                .Select(item => _profileFactory.Generate(item, lastUpdateDate, ProfileUpdateApiFlag.LookupCircle)));

            return Tuple.Create(resCircles.ToArray(), resProfiles.ToArray());
        }
        public async Task<ProfileData> GetProfileLiteAsync(string profileId, IPlatformClient client)
        {
            var apiResponseTxt = await _apiWrapper.ConnectToLookupPeople(client.NormalHttpClient, client.PlusBaseUrl, new[]{ profileId }, client.AtValue);
            var apiResponse = JToken.Parse(ApiAccessorUtility.ConvertIntoValidJson(apiResponseTxt));
            var lastUpdateDate = DateTime.UtcNow;
            return _profileFactory.Generate(apiResponse[0][1][2][0], lastUpdateDate, ProfileUpdateApiFlag.LookupProfile);
        }
        public async Task<ProfileData> GetProfileFullAsync(string profileId, IPlatformClient client)
        {
            var apiResponseTxt = await _apiWrapper.ConnectToProfileGet(client.NormalHttpClient, client.PlusBaseUrl, profileId);
            var apiResponse = JToken.Parse(ApiAccessorUtility.ConvertIntoValidJson(apiResponseTxt));
            var lastUpdateDate = DateTime.UtcNow;
            return _profileFactory.Generate(apiResponse[0][1][1][2], lastUpdateDate, ProfileUpdateApiFlag.ProfileGet);
        }
        public async Task<ProfileData> GetProfileAboutMeAsync(IPlatformClient client)
        {
            var json = JToken.Parse(ApiAccessorUtility.ConvertIntoValidJson(
                await _apiWrapper.ConnectToGetIdentities(client.NormalHttpClient, client.PlusBaseUrl)));
            var id = (string)json[0][0][1];
            var lastUpdateDate = DateTime.UtcNow;
            if (id == null)
                throw new ApiErrorException("自身のPlusID取得に失敗しました。ログインされていない可能性があります。", ErrorType.SessionError, null, null, null, null);
            return _profileFactory.Generate(json[0][1][1][0], lastUpdateDate, ProfileUpdateApiFlag.ProfileGet);
        }
        public async Task<ProfileData[]> GetFollowingProfilesAsync(string profileId, IPlatformClient client)
        {
            var results = new List<ProfileData>();
            var apiResponseTxt = await _apiWrapper.ConnectToLookupVisible(client.NormalHttpClient, client.PlusBaseUrl, profileId, client.AtValue);
            var apiResponse = JToken.Parse(ApiAccessorUtility.ConvertIntoValidJson(apiResponseTxt));
            foreach (var item in apiResponse[0][1][2])
                results.Add(new ProfileData(
                    (string)item[0][2], (string)item[2][0], ApiAccessorUtility.ConvertReplasableUrl((string)item[2][8]),
                    greetingText: (string)item[2][21], loadedApiTypes: ProfileUpdateApiFlag.Base));
            return results.ToArray();
        }
        public async Task<ProfileData[]> GetFollowedProfilesAsync(string profileId, int count, IPlatformClient client)
        {
            var results = new List<ProfileData>();
            var apiResponseTxt = await _apiWrapper.ConnectToLookupIncoming(client.NormalHttpClient, client.PlusBaseUrl, profileId, count, client.AtValue);
            var apiResponse = JToken.Parse(ApiAccessorUtility.ConvertIntoValidJson(apiResponseTxt));
            foreach (var item in apiResponse[0][1][2])
            {
                var iconUrl = item[2][8].Type != JTokenType.Null ? (string)item[2][8] : null;
                results.Add(new ProfileData(
                    (string)item[0][2], (string)item[2][0], ApiAccessorUtility.ConvertReplasableUrl(iconUrl),
                    greetingText: (string)item[2][21], loadedApiTypes: ProfileUpdateApiFlag.Base));
            }
            return results.ToArray();
        }
        public async Task<ProfileData[]> GetIgnoredProfilesAsync(IPlatformClient client)
        {
            var apiResponseTxt = await _apiWrapper.ConnectToLookupIgnore(client.NormalHttpClient, client.PlusBaseUrl, client.AtValue);
            var apiResponse = JToken.Parse(ApiAccessorUtility.ConvertIntoValidJson(apiResponseTxt));
            var ignoreLst = new List<ProfileData>();
            foreach (var item in apiResponse[0][1][2])
            {
                var profileId = (string)item[0].ElementAtOrDefault(2);
                var iconUrl = ApiAccessorUtility.ConvertReplasableUrl((string)item[2][8]);
                var profile = new ProfileData(profileId, (string)item[2][0], iconUrl, loadedApiTypes: ProfileUpdateApiFlag.Base);
                ignoreLst.Add(profile);
            }
            return ignoreLst.ToArray();
        }
        public async Task<ProfileData[]> GetFollowingMeProfilesAsync(IPlatformClient client)
        {
            var apiResponseTxt = await _apiWrapper.ConnectToLookupFollowers(client.NormalHttpClient, client.PlusBaseUrl, client.BuildLevel, client.Afsid, client.Lang, client.AtValue);
            var apiResponse = JToken.Parse(ApiAccessorUtility.ConvertIntoValidJson(apiResponseTxt))[0][1][1][0][1];
            var followersLst = new List<ProfileData>();
            foreach (var item in apiResponse)
            {
                var jsonTmp = item[1];
                var profileId = (string)jsonTmp[0][2];
                jsonTmp = jsonTmp[2];
                var iconUrl = ApiAccessorUtility.ConvertReplasableUrl((string)jsonTmp[8]);
                followersLst.Add(new ProfileData(
                    profileId, (string)jsonTmp[0], iconUrl, loadedApiTypes: ProfileUpdateApiFlag.Base));
            }
            return followersLst.ToArray();
        }
        public async Task<ProfileData[]> GetProfileOfPusherAsync(string plusOneId, int pushCount, IPlatformClient client)
        {
            var resLst = new List<ProfileData>();
            var json = JToken.Parse(ApiAccessorUtility.ConvertIntoValidJson(
                await _apiWrapper.ConnectToCommonGetPeople(client.NormalHttpClient, client.PlusBaseUrl, plusOneId, pushCount, client.AtValue)))[0][1][1];
            foreach (var item in json)
                resLst.Add(new ProfileData(
                    (string)item[1], (string)item[0], ApiAccessorUtility.ConvertReplasableUrl((string)item[3]),
                    status: AccountStatus.Active, loadedApiTypes: ProfileUpdateApiFlag.Base));
            return resLst.ToArray();
        }
        public async Task<ActivityData> GetActivityAsync(string activityId, IPlatformClient client)
        {
            var apiResponseTxt = await _apiWrapper.ConnectToGetActivity(client.NormalHttpClient, client.PlusBaseUrl, activityId, client.Lang, client.BuildLevel, client.Afsid);
            var apiResponse = JToken.Parse(ApiAccessorUtility.ConvertIntoValidJson(apiResponseTxt));
            return _activityFactory.Generate(apiResponse[0][1][1], ActivityUpdateApiFlag.GetActivity, client);
        }
        public async Task<Tuple<ActivityData[], string>> GetActivitiesAsync(string circleId, string profileId, string ctValue, int length, IPlatformClient client)
        {
            var activities = new List<ActivityData>();
            var nowDate = DateTime.UtcNow;
            for (var i = 0; i < length; )
            {
                var oneSetSize = Math.Min(length, 40);
                var oneSetCount = 0;
                var apiResponseTxt = await _apiWrapper.ConnectToGetActivities(client.NormalHttpClient, client.PlusBaseUrl, client.AtValue, oneSetSize, circleId, profileId, ctValue);
                var apiResponse = JToken.Parse(ApiAccessorUtility.ConvertIntoValidJson(apiResponseTxt))[0][1][1];
                ctValue = (string)apiResponse[1];
                foreach (var item in apiResponse[0])
                {
                    oneSetCount++;
                    activities.Add(_activityFactory.Generate(item, ActivityUpdateApiFlag.GetActivities, client));
                    if (++i >= length)
                        break;
                }
                //一度にsingleSize分だけ読み込む。同時にsingleLengthで実際何件読み込んだかを記録し、
                //期待した件数と実際のを比べて違ったらそれ以上読み込めないとして切り上げる
                if (oneSetCount < oneSetSize)
                    break;
            }
            return Tuple.Create(activities.ToArray(), ctValue);
        }
        public async Task<Tuple<NotificationData[], DateTime, string>> GetNotificationsAsync(bool isFetchNewItemMode, int length, string continueToken, IPlatformClient client)
        {
            const int MAX_RESULT_LENGTH = 30;
            var notificationList = new List<NotificationData>();
            var latestReadedItemNotifiedDate = DateTime.MinValue;
            do
            {
                var apiResponseTxt = await _apiWrapper.ConnectToNotificationsFetch(client.NormalHttpClient, client.PlusBaseUrl, isFetchNewItemMode, client.AtValue, Math.Min(length, MAX_RESULT_LENGTH), continueToken);
                var apiResponse = JToken.Parse(ApiAccessorUtility.ConvertIntoValidJson(apiResponseTxt))[0][1];
                continueToken = apiResponse[2].Type == JTokenType.Array ? (string)apiResponse[2][5] : null;
                if (latestReadedItemNotifiedDate == DateTime.MinValue)
                    latestReadedItemNotifiedDate = ApiWrapper.GetDateTime((ulong)apiResponse[4] / 1000);
                foreach (var item in apiResponse[1])
                {
                    var data = _notificationFactory.Generate(item, client.PlusBaseUrl);
                    notificationList.Add(data);
                    length--;
                }
            }
            while (length > 0 && continueToken != null);
            return Tuple.Create(notificationList.ToArray(), latestReadedItemNotifiedDate, continueToken);
        }
        public async Task<int> GetUnreadNotificationCountAsync(IPlatformClient client)
        {
            var json = JToken.Parse(ApiAccessorUtility.ConvertIntoValidJson(
                await _apiWrapper.ConnectToGsuc(client.NormalHttpClient, client.PlusBaseUrl)));
            return (int)json[0];
        }
        public async Task<AlbumData> GetAlbumAsync(string albumId, string profileId, IPlatformClient client)
        {
            var json = JToken.Parse(ApiAccessorUtility.ConvertIntoValidJson(
                await _apiWrapper.ConnectToPhotosAlbums(client.NormalHttpClient, client.PlusBaseUrl, profileId, albumId)));
            return GenerateAlbumData(json[0][1][1], json[0][1][2], AlbumUpdateApiFlag.Full, client.PlusBaseUrl);
        }
        public async Task<AlbumData[]> GetAlbumsAsync(string profileId, IPlatformClient client)
        {
            var json = JToken.Parse(await _apiWrapper.ConnectToPhotosAlbums(client.NormalHttpClient, client.PlusBaseUrl, profileId));
            var resultAlbums = new List<AlbumData>();
            foreach (var item in json[0][1][2])
                resultAlbums.Add(GenerateAlbumData(item, null, AlbumUpdateApiFlag.Base, client.PlusBaseUrl));
            return resultAlbums.ToArray();
        }
        public async Task<ImageData> GetImageAsync(string imageId, string profileId, IPlatformClient client)
        {
            var json = JToken.Parse(ApiAccessorUtility.ConvertIntoValidJson(
                await _apiWrapper.ConnectToPhotosLightbox(client.NormalHttpClient, client.PlusBaseUrl, profileId, imageId)))[1][1];
            var tempJson = json[7];
            var owner = new ProfileData(
                (string)tempJson[0], (string)tempJson[3], ApiAccessorUtility.ConvertReplasableUrl((string)tempJson[4]),
                loadedApiTypes: ProfileUpdateApiFlag.Base);
            var albumInfo = GenerateAlbumData(json[11], null, AlbumUpdateApiFlag.Base, client.PlusBaseUrl);
            var name = (string)json[8];
            tempJson = json[25];
            var imageUrl = (string)tempJson[0];
            var width = (int)tempJson[1];
            var height = (int)tempJson[2];
            var isolateActivity = _activityFactory.Generate(json[10], ActivityUpdateApiFlag.GetActivity, client);
            var tagArray = tempJson[6]
                .Select(item =>
                    {
                        //未完成
                        var ownerJson = item[10];
                        var tagContentJson = item[7][0];
                        if ((int)item[9] == 1)
                            return (ImageTagData)new ImageMensionTagInfo(
                                (int)item[1], (int)item[2], (int)item[3], (int)item[4],
                                new ProfileData(
                                    (string)tagContentJson[0], (string)tagContentJson[3],
                                    ApiAccessorUtility.ConvertReplasableUrl((string)tagContentJson[8]),
                                    status: AccountStatus.Active, loadedApiTypes: ProfileUpdateApiFlag.Base),
                                new ProfileData(
                                    (string)ownerJson[0], (string)ownerJson[4],
                                    ApiAccessorUtility.ConvertReplasableUrl((string)ownerJson[8]),
                                    status: AccountStatus.Active, loadedApiTypes: ProfileUpdateApiFlag.Base));
                        else
                            return (ImageTagData)new ImageTextTagInfo(
                                (int)item[1], (int)item[2], (int)item[3], (int)item[4], (string)tagContentJson[3],
                                new ProfileData(
                                    (string)ownerJson[0], (string)ownerJson[3], ApiAccessorUtility.ConvertReplasableUrl((string)ownerJson[8]),
                                    status: AccountStatus.Active, loadedApiTypes: ProfileUpdateApiFlag.Base));
                    })
                .ToArray();
            return new ImageData(ImageUpdateApiFlag.LightBox, imageId, name, width, height, imageUrl, null, null, tagArray, owner, isolateActivity);
        }
        public IObservable<object> GetStreamAttacher(IPlatformClient client)
        {
            return _apiWrapper
                .ConnectToTalkGadgetBind(client.NormalHttpClient, client.StreamHttpClient, client.TalkBaseUrl, client.Cookies, client.PvtValue)
                .Where(json => (string)json[1][0] == "c")
                .Select(json => GenerateDataFromStreamingApi(json, client));
        }
        public async Task MarkAsReadAsync(NotificationData target, IPlatformClient client)
        {
            await _apiWrapper.ConnectToSetReadStates(
                client.NormalHttpClient, client.PlusBaseUrl, target.Id, target.RawNoticedDate, client.AtValue);
            if (target is ContentNotificationData)
                await _apiWrapper.ConnectToMarkItemRead(
                    client.NormalHttpClient, client.PlusBaseUrl,
                    ((ContentNotificationData)target).Target.Id, client.AtValue);
        }
        public async Task<ActivityData> PostActivity(string content, Dictionary<string, string> targetCircles, Dictionary<string, string> targetUsers, bool isDisabledComment, bool isDisabledReshare, IPlatformClient client)
        {
            var apiResponse = JToken.Parse(ApiAccessorUtility.ConvertIntoValidJson(await _apiWrapper.ConnectToPost(
                client.NormalHttpClient, client.PlusBaseUrl, DateTime.Now, 0, null, targetCircles, targetUsers, null, content, isDisabledComment, isDisabledReshare, client.AtValue)));
            return _activityFactory.Generate(apiResponse, ActivityUpdateApiFlag.GetActivity, client);
        }
        public async Task<CommentData> PostComment(string activityId, string content, IPlatformClient client)
        {
            var apiResponse = JToken.Parse(ApiAccessorUtility.ConvertIntoValidJson(
                await _apiWrapper.ConnectToComment(client.NormalHttpClient, client.PlusBaseUrl, activityId, content, DateTime.Now, client.AtValue)));
            return _commentFactory.Generate(apiResponse[0][1][1]);
        }
        public async Task<CommentData> EditComment(string activityId, string commentId, string content, IPlatformClient client)
        {
            var apiResponse = JToken.Parse(ApiAccessorUtility.ConvertIntoValidJson(
                await _apiWrapper.ConnectToEditComment(client.NormalHttpClient, client.PlusBaseUrl, activityId, commentId, content, client.AtValue)));
            return _commentFactory.Generate(apiResponse[0][1][1]);
        }
        public Task DeleteComment(string commentId, IPlatformClient client)
        { return _apiWrapper.ConnectToDeleteComment(client.NormalHttpClient, client.PlusBaseUrl, commentId, client.AtValue); }
        public Task MutateBlockUser(Tuple<string, string>[] userIdAndNames, AccountBlockType blockType, BlockActionType status, IPlatformClient client)
        { return _apiWrapper.ConnectToMutateBlockUser(client.NormalHttpClient, client.PlusBaseUrl, userIdAndNames, blockType, status, client.AtValue); }

        ProfileDataFactory IDataFactoryManager.ProfileFactory { get { return _profileFactory; } }
        ActivityDataFactory IDataFactoryManager.ActivityFactory { get { return _activityFactory; } }
        CommentDataFactory IDataFactoryManager.CommentFactory { get { return _commentFactory; } }
        AttachedDataFactory IDataFactoryManager.AttachedFactory { get { return _attachedFactory; } }
        NotificationDataFactory IDataFactoryManager.NotificationFactory { get { return _notificationFactory; } }

        static AlbumData GenerateAlbumData(JToken albumApiResponse, JToken imageApiResponse, AlbumUpdateApiFlag loadedApiTypes, Uri plusBaseUrl)
        {
            var id = (string)albumApiResponse[5];
            var name = (string)albumApiResponse[2];
            var albumUrl = new Uri((string)albumApiResponse[8]);
            var owner = new ProfileData(
                (string)albumApiResponse[13][0], (string)albumApiResponse[13][3], ApiAccessorUtility.ConvertReplasableUrl((string)albumApiResponse[13][4]),
                loadedApiTypes: ProfileUpdateApiFlag.Base);
            var spltAry = ((string)albumApiResponse[7]).Split('E');
            var createDate = ApiWrapper.GetDateTime((ulong)(double.Parse(spltAry[0]) * Math.Pow(10.0, (double.Parse(spltAry[1]) + 3))));
            var attachedActivityId = (string)null;
            var picLst = new List<ImageData>();
            if (imageApiResponse != null)
                foreach (var itemA in imageApiResponse)
                    if (itemA != null)
                        picLst.Add(GenerateImageData(itemA, ImageUpdateApiFlag.Base, plusBaseUrl));
            var bookCovers = picLst.ToArray();

            return new AlbumData(id, name, albumUrl, createDate, bookCovers, bookCovers, attachedActivityId, owner, loadedApiTypes);
        }
        static ImageData GenerateImageData(JToken apiResponse, ImageUpdateApiFlag loadedApiTypes, Uri plusBaseUrl)
        {
            var id = (string)apiResponse[5];
            var name = (string)apiResponse[8];
            var imageUrl = ApiAccessorUtility.ConvertReplasableUrl((string)apiResponse[2][0]);
            var width = ((JArray)apiResponse[2]).Count >= 2 ? (int)apiResponse[2][1] : -1;
            var height = ((JArray)apiResponse[2]).Count >= 3 ? (int)apiResponse[2][2] : -1;
            var picasaUrl = new Uri((string)apiResponse[0]);
            var createDate = ((JValue)apiResponse[15]).Value != null ? new Nullable<DateTime>(ApiWrapper.GetDateTime((ulong)apiResponse[15])) : null;
            var owner = apiResponse[7].Type == JTokenType.Null ? null : new ProfileData(
                (string)apiResponse[7][0], (string)apiResponse[7][3], ApiAccessorUtility.ConvertReplasableUrl((string)apiResponse[7][4]),
                loadedApiTypes: ProfileUpdateApiFlag.Base);
            var tags = new List<ImageTagData>();
            foreach (var item in apiResponse[6])
            {
                var locationJson = item[3];
                var leftTop = locationJson[0];
                var rightBottom = locationJson[1];
                var ownerJson = item[4];
                var contentJson = item[1];
                var tagOwner = new ProfileData(
                    (string)ownerJson[0], (string)ownerJson[3], ApiAccessorUtility.ConvertReplasableUrl((string)ownerJson[4]),
                    status: AccountStatus.Active, loadedApiTypes: ProfileUpdateApiFlag.Base);
                if (contentJson[1] != null)
                    tags.Add(new ImageMensionTagInfo(
                        (int)leftTop[0], (int)leftTop[1], (int)rightBottom[0], (int)rightBottom[1],
                        new ProfileData((string)contentJson[0], (string)contentJson[3], ApiAccessorUtility.ConvertReplasableUrl((string)contentJson[4])),
                        tagOwner));
                else
                    tags.Add(new ImageTextTagInfo(
                        (int)leftTop[0], (int)leftTop[1], (int)rightBottom[0],
                        (int)rightBottom[1], (string)contentJson[3], tagOwner));
            }
            var attachedTags = tags.ToArray();
            Uri linkUrl = null;
            if (((JArray)apiResponse[64]).Count > 0)
                linkUrl = new Uri(plusBaseUrl, (string)apiResponse[64][0][3]);

            return new ImageData(loadedApiTypes, id, name, width, height, imageUrl, linkUrl, createDate, attachedTags, owner);
        }
        object GenerateDataFromStreamingApi(JToken rawItem, IPlatformClient client)
        {
            if (rawItem[1][1].Type != JTokenType.Array)
                return rawItem;
            var json = rawItem[1][1][1];
            switch ((string)json[0])
            {
                case "rtu":
                    var shrItm = JArray.Parse(ApiAccessorUtility.ConvertIntoValidJson((string)json[1]));
                    if ((string)shrItm[0] != "t.rtnr")
                    {
                        System.Diagnostics.Debug.Assert(false, "talkgadgetBindに想定外のjsonが入ってきました。");
                        return rawItem;
                    }
                    PostStatusType status;
                    switch ((int)shrItm[2])
                    {
                        case 1: status = PostStatusType.First; break;
                        case 2: status = PostStatusType.Edited; break;
                        case 3: status = PostStatusType.Removed; break;
                        default:
                            System.Diagnostics.Debug.Assert(false, "talkgadgetBindに想定外のjsonが入ってきました。PostStatusType用の(int)shrItm[2]に未知の値が入っています。");
                            return rawItem;
                    }
                    switch ((int)shrItm[1])
                    {
                        case 2:
                            if (status != PostStatusType.Removed)
                                return _commentFactory.Generate(shrItm[6]);
                            else
                            {
                                var cid = (string)shrItm[4];
                                var aid = (string)shrItm[3];
                                return new CommentData(cid, aid, null, DateTime.MinValue, DateTime.MinValue, null, PostStatusType.Removed);
                            }
                        case 1:
                            if (status != PostStatusType.Removed)
                                return new ActivityData(
                                    (string)shrItm[3],
                                    status: PostStatusType.First, owner: new ProfileData((string)shrItm[7]),
                                    updaterTypes: ActivityUpdateApiFlag.Base);
                            else
                                return new ActivityData((string)shrItm[3], status: PostStatusType.Removed, updaterTypes: ActivityUpdateApiFlag.GetActivities);
                        default:
                            System.Diagnostics.Debug.Assert(false, "talkgadgetBindに想定外のjsonが入ってきました。");
                            return rawItem;
                    }
                case "gb":
                    var notificationItem = JArray.Parse(
                        ApiAccessorUtility.ConvertIntoValidJson((string)json[1]));
                    switch ((string)notificationItem[0])
                    {
                        case "gb.n.rtn":
                            return new NotificationSignal(NotificationEventType.RaiseNew, (string)notificationItem[1]);
                        case "gb.n.sup":
                            return new NotificationSignal(NotificationEventType.ChangedAllRead, null);
                        default:
                            return notificationItem;
                    }
                default:
                    return rawItem;
            }
        }
        static string GetProfileId(JToken apiResponse, ProfileUpdateApiFlag apiType)
        {
            switch (apiType)
            {
                case ProfileUpdateApiFlag.LookupProfile:
                case ProfileUpdateApiFlag.LookupCircle:
                    {
                        var profileJson = apiResponse[2];
                        var status = ((JValue)profileJson[10]).Value != null ? AccountStatus.Active : AccountStatus.MailOnly;
                        if (apiResponse[0].ElementAtOrDefault(2) == null && status != AccountStatus.MailOnly)
                            throw new ArgumentException("引数json内にProfileInfo.Idの情報が含まれていないものを指定することはできません。");

                        return (string)apiResponse[0].ElementAtOrDefault(2) ?? (string)apiResponse[0][0];
                    }
                case ProfileUpdateApiFlag.ProfileGet:
                    {
                        var tmpJsonA = apiResponse[4];
                        return (string)apiResponse[30];
                    }
                default:
                    throw new ArgumentException("引数apiTypeに予想外の値が代入されていました。");
            }
        }
    }
    public class ProfileDataFactory : DataFactory<ProfileData>
    {
        public ProfileDataFactory(IDataFactoryManager accessor) : base(accessor) { }
        public ProfileData Generate(JToken apiResponse, DateTime? lastUpdateDate, ProfileUpdateApiFlag apiType)
        {
            switch (apiType)
            {
                case ProfileUpdateApiFlag.LookupProfile:
                case ProfileUpdateApiFlag.LookupCircle:
                    {
                        var profileJson = apiResponse[2];
                        var status = ((JValue)profileJson[10]).Value != null
                            ? (string)profileJson[5] != "LK_OLF" ? AccountStatus.Active : AccountStatus.Disable
                            : AccountStatus.MailOnly;
                        if (apiResponse[0].ElementAtOrDefault(2) == null && status != AccountStatus.MailOnly)
                            throw new ArgumentException("引数json内にProfileInfo.Idの情報が含まれていないものを指定することはできません。");

                        var id = (string)apiResponse[0].ElementAtOrDefault(2) ?? (string)apiResponse[0][0];
                        var name = (string)profileJson[0];
                        var greetingText = (string)profileJson.ElementAtOrDefault(21);
                        var iconImageUrl = ApiAccessorUtility.ConvertReplasableUrl((string)profileJson[8]);
                        var circleIds = apiResponse[3] != null ? apiResponse[3].Select(cidItm => (string)cidItm[2][0]).ToArray() : null;

                        return new ProfileData(
                            id, name, iconImageUrl: iconImageUrl, status: status, greetingText: greetingText,
                            loadedApiTypes: apiType,
                            lastUpdateLookupProfile: apiType == ProfileUpdateApiFlag.LookupProfile ? lastUpdateDate : null,
                            lastUpdateProfileGet: apiType == ProfileUpdateApiFlag.ProfileGet ? lastUpdateDate : null);
                    }
                case ProfileUpdateApiFlag.ProfileGet:
                    {
                        JToken tmpJsonA;
                        var id = (string)apiResponse[30];
                        tmpJsonA = apiResponse[4];
                        var name = (string)tmpJsonA[3];
                        var firstName = (string)tmpJsonA[1];
                        var lastName = (string)tmpJsonA[2];
                        var introduction = (string)apiResponse[14][1];
                        tmpJsonA = apiResponse[19];
                        var braggingRights = (string)tmpJsonA.ElementAtOrDefault(1) ?? string.Empty;
                        tmpJsonA = apiResponse[6];
                        var occupation = (string)tmpJsonA.ElementAtOrDefault(1) ?? string.Empty;
                        var employments = apiResponse[7][1].Select(item => new EmploymentInfo(item)).ToArray();
                        var educations = apiResponse[8][1].Select(item => new EducationInfo(item)).ToArray();
                        var placesLived = apiResponse[9][2].Select(token => (string)token).ToArray();
                        var lists = new[] { new List<ContactInfo>(), new List<ContactInfo>() };
                        for (var i = 0; i < lists.Length; i++)
                        {
                            var contactJson = apiResponse[12 + i];
                            foreach (string info in contactJson[1])
                                lists[i].Add(new ContactInfo(info, ContactType.Phone));
                            foreach (string info in contactJson[2])
                                lists[i].Add(new ContactInfo(info, ContactType.Mobile));
                            foreach (string info in contactJson[3])
                                lists[i].Add(new ContactInfo(info, ContactType.Fax));
                            foreach (string info in contactJson[4])
                                lists[i].Add(new ContactInfo(info, ContactType.Pager));
                            foreach (string info in contactJson[6])
                                lists[i].Add(new ContactInfo(info, ContactType.Adress));
                            foreach (var info in contactJson[7])
                            {
                                var address = (string)info[0];
                                var typeId = (int)(long)((JValue)info[1]).Value;
                                var infoType = typeId < 2 || typeId > 10 ? ContactType.Unknown : (ContactType)typeId;
                                var contactInfo = new ContactInfo(address, infoType);
                                lists[i].Add(contactInfo);
                            }
                            foreach (var info in contactJson[9])
                                lists[i].Add(new ContactInfo((string)info[0], ContactType.Email));
                        }
                        var contactsInHome = lists[0].ToArray();
                        var contactsInWork = lists[1].ToArray();
                        tmpJsonA = apiResponse[22];
                        var relationship = (RelationType)(int.Parse((string)tmpJsonA.ElementAtOrDefault(1) ?? "-1"));
                        var lookingFor = new LookingFor(apiResponse[23]);
                        var greetingText = (string)apiResponse[33][1];
                        tmpJsonA = apiResponse[17];
                        var genderType = (GenderType)(int.Parse((string)tmpJsonA.ElementAtOrDefault(1) ?? "-1"));
                        var otherNames = apiResponse[5][1].Select(token => (string)token[0]).ToArray();
                        tmpJsonA = apiResponse[47];
                        var nickName = (string)tmpJsonA.ElementAtOrDefault(1) ?? string.Empty;

                        List<UrlInfo>[] urlLists = new[] { new List<UrlInfo>(), new List<UrlInfo>(), new List<UrlInfo>() };
                        for (var i = 0; i < urlLists.Length; i++)
                            foreach (var urlList in apiResponse[51 + i][0])
                                urlLists[i].Add(new UrlInfo(urlList));
                        var otherProfileUrls = urlLists[0].ToArray();
                        var contributeUrls = urlLists[1].ToArray();
                        var recommendedUrls = urlLists[2].ToArray();
                        var iconImageUrl = ApiAccessorUtility.ConvertReplasableUrl((string)apiResponse[3]);

                        return new ProfileData(
                            id, name, iconImageUrl, AccountStatus.Active, firstName, lastName, introduction, braggingRights,
                            occupation, greetingText, nickName, relationship, genderType, lookingFor, employments, educations,
                            contactsInHome, contactsInWork, otherProfileUrls, contributeUrls, recommendedUrls, placesLived,
                            otherNames, apiType,
                            apiType == ProfileUpdateApiFlag.LookupProfile ? lastUpdateDate : null,
                            apiType == ProfileUpdateApiFlag.ProfileGet ? lastUpdateDate : null);
                    }
                default:
                    throw new ArgumentException("引数apiTypeに予想外の値が代入されていました。");
            }
        }
        public override void GetStubModeConfig(Dictionary<Expression<Func<ProfileData, object>>, object> config, string marker)
        {
            config.Add(dt => dt.Status, AccountStatus.Active);
            config.Add(dt => dt.Status, AccountStatus.Active);
            config.Add(dt => dt.LoadedApiTypes, ProfileUpdateApiFlag.Base);
            config.Add(dt => dt.Relationship, RelationType.Engaged);
            config.Add(dt => dt.Gender, GenderType.Other);
            base.GetStubModeConfig(config, marker);
        }
    }
    public class ActivityDataFactory : DataFactory<ActivityData>
    {
        public ActivityDataFactory(IDataFactoryManager accessor) : base(accessor) { }
        public ActivityData Generate(JToken apiResponse, ActivityUpdateApiFlag loadedApiTypes, IPlatformClient client)
        {
            var updateDate = DateTime.UtcNow;
            var id = (string)apiResponse[8];
            var postUserId = (string)apiResponse[16];
            var postUserName = (string)apiResponse[3];
            var postUserIconUrl = ApiAccessorUtility.ConvertReplasableUrl((string)apiResponse[18]);
            var postUrl = new Uri(client.PlusBaseUrl, (string)apiResponse[21]);
            var postDate = ApiWrapper.GetDateTime((ulong)apiResponse[5]);
            var lastUpdateDate = ApiWrapper.GetDateTime((ulong)apiResponse[30] / 1000);
            var isEditable = (double)apiResponse[56] == 1.0;
            var serviceType = new ServiceType((string)apiResponse[10]);
            var commentLength = (int)apiResponse[93];
            var comments = apiResponse[7].Select(itm => Manager.CommentFactory.Generate(itm)).ToArray();

            //初版かどうかの判定
            DateTime editDate;
            PostStatusType postStatus;
            if ((string)apiResponse[70] != null)
            {
                editDate = ApiWrapper.GetDateTime(ulong.Parse((string)apiResponse[70]) / 1000);
                postStatus = PostStatusType.Edited;
            }
            else
            {
                editDate = postDate;
                postStatus = PostStatusType.First;
            }

            //添付されたコンテンツの判定
            //IAttachable atchCnt = null;
            IAttachable atchCnt = apiResponse[97].Type == JTokenType.Array && ((JArray)apiResponse[97]).Count > 0
                ? Manager.AttachedFactory.Generate((JArray)apiResponse[97], client.PlusBaseUrl) : null;
            //再共有投稿の場合はデータの並びが一部異なるため、処理を分ける
            string html, text;
            StyleElement element;
            IAttachable attachedContent;
            if (apiResponse[39].Type == JTokenType.String)
            {
                html = (string)apiResponse[47];
                text = (string)apiResponse[48];
                element = ContentElement.ParseHtml(html, client) ?? ContentElement.ParseJson(apiResponse[139]);
                var attachedHtml = (string)apiResponse[4];
                var attachedText = (string)apiResponse[20];
                var attachedElement = ContentElement.ParseHtml(attachedHtml, client) ?? ContentElement.ParseJson(apiResponse[137]);
                attachedContent = new AttachedPostData(
                    ContentType.Reshare, (string)apiResponse[40], attachedHtml, attachedText, attachedElement,
                    (string)apiResponse[44][1], (string)apiResponse[44][0],
                    ApiAccessorUtility.ConvertReplasableUrl((string)apiResponse[44][4]),
                    new Uri(client.PlusBaseUrl, (string)apiResponse[77]), atchCnt);
            }
            else
            {
                html = (string)apiResponse[4];
                text = (string)apiResponse[20];
                element = ContentElement.ParseHtml(html, client) ?? ContentElement.ParseJson(apiResponse[137]);
                attachedContent = atchCnt;
            }

            return new ActivityData(
                id, html, text, element, isEditable, postUrl, commentLength, comments, postDate, editDate, serviceType, postStatus, attachedContent,
                new ProfileData(postUserId, postUserName, postUserIconUrl, loadedApiTypes: ProfileUpdateApiFlag.Base),
                updateDate, loadedApiTypes);
        }
        public override void GetStubModeConfig(Dictionary<Expression<Func<ActivityData, object>>, object> config, string marker)
        {
            config.Add(dt => dt.LoadedApiTypes, ActivityUpdateApiFlag.Unloaded);
            config.Add(dt => dt.IsEditable, false);
            config.Add(dt => dt.PostDate, new DateTime(2014, 1, 23));
            config.Add(dt => dt.EditDate, new DateTime(2014, 1, 24));
            config.Add(dt => dt.GetActivityDate, new DateTime(2014, 1, 25));
            config.Add(dt => dt.PostStatus, PostStatusType.First);
            config.Add(dt => dt.ServiceType, ServiceType.Desktop);
            config.Add(dt => dt.CommentLength, 8);
            base.GetStubModeConfig(config, marker);
        }
    }
    public class CommentDataFactory : DataFactory<CommentData>
    {
        public CommentDataFactory(IDataFactoryManager accessor) : base(accessor) { }
        public CommentData Generate(JToken apiResponse)
        {
            var cid = (string)apiResponse[4];
            var aid = (string)apiResponse[7];
            var chtml = (string)apiResponse[2];
            //PlusOne = new PlusOneInfo(client, this);
            //PlusOne.Parse(json[15]);

            var commentDate = ApiWrapper.GetDateTime((ulong)apiResponse[3]);
            var ownerId = (string)apiResponse[6];
            var ownerName = (string)apiResponse[1];
            var ownerIconUrl = ApiAccessorUtility.ConvertReplasableUrl((string)apiResponse[16]);
            PostStatusType status;
            DateTime cEditDate;
            if ((long)((JValue)apiResponse[14]).Value != 0)
            {
                cEditDate = ApiWrapper.GetDateTime((ulong)apiResponse[14]);
                status = PostStatusType.Edited;
            }
            else
            {
                cEditDate = DateTime.MaxValue;
                status = PostStatusType.First;
            }
            return new CommentData(
                cid, aid, chtml, commentDate, cEditDate,
                new ProfileData(ownerId, ownerName, ownerIconUrl, loadedApiTypes: ProfileUpdateApiFlag.Base),
                status);
        }
        public override void GetStubModeConfig(Dictionary<Expression<Func<CommentData, object>>, object> config, string marker)
        {
            config.Add(dt => dt.PostDate, DateTime.UtcNow);
            config.Add(dt => dt.EditDate, DateTime.MinValue);
            config.Add(dt => dt.Status, PostStatusType.First);
            base.GetStubModeConfig(config, marker);
        }
    }
    public class AttachedDataFactory : DataFactory<AttachedBase>
    {
        public AttachedDataFactory(IDataFactoryManager accessor) : base(accessor) { }
        public virtual AttachedBase Generate(JArray attachedContentJson, Uri plusBaseUrl)
        {
            AttachedBase content;
            var prop = GetContentBody(attachedContentJson);
            var body = (JArray)prop.Value;
            switch (prop.Name)
            {
                case "40154698":
                case "39748951":
                case "42861421":
                case "42397230":
                    //リンク
                    content = new AttachedLink(ContentType.Link,
                        ParseTitle(body), ParseSummary(body), ParseLinkUrl(body, plusBaseUrl), ParseFaviconUrl(body),
                        ParseOriginalThumbnailUrl(body), ParseThumbnailUrl(body), ParseThumbnailWidth(body),
                        ParseThumbnailHeight(body));
                    break;
                case "41561070":
                    //インタラクティブ
                    content = new AttachedInteractiveLink(ContentType.InteractiveLink,
                        ParseTitle(body), ParseSummary(body), ParseLinkUrl(body, plusBaseUrl), ParseProviderName(body),
                        ParseProviderLogoUrl(body), ParseActionUrl(body), ParseLabel(body), ParseFaviconUrl(body),
                        ParseOriginalThumbnailUrl(body), ParseThumbnailUrl(body), ParseThumbnailWidth(body),
                        ParseThumbnailHeight(body), plusBaseUrl);
                    break;
                case "40655821":
                    //写真
                    content = new AttachedImageData(ContentType.Image,
                        ParseTitle(body), ParseSummary(body), ParseLinkUrl(body, plusBaseUrl), ParseFaviconUrl(body),
                        ParseOriginalThumbnailUrl(body), ParseThumbnailUrl(body), ParseThumbnailWidth(body),
                        ParseThumbnailHeight(body), ParseImage(body, plusBaseUrl), ParseAlbum(body, plusBaseUrl),
                        plusBaseUrl);
                    break;
                case "40842909":
                    //アルバム
                    content = new AttachedAlbumData(ContentType.Album,
                        ParseLinkUrl(body, plusBaseUrl), ParseAlbum(body, plusBaseUrl),
                        ParsePictures(body, plusBaseUrl), plusBaseUrl);
                    break;
                case "41186541":
                    //youtube
                    content = new AttachedYouTube(ContentType.YouTube,
                        ParseTitle(body), ParseSummary(body), ParseLinkUrl(body, plusBaseUrl), ParseEmbedMovieUrl(body),
                        ParseFaviconUrl(body), ParseOriginalThumbnailUrl(body), ParseThumbnailUrl(body),
                        ParseThumbnailWidth(body), ParseThumbnailHeight(body), plusBaseUrl);
                    break;
                case "41359510":
                    //現在地共有
                    content = null;
                    break;
                default:
                    content = null;
                    System.Diagnostics.Debug.WriteLine(string.Format("未確認の添付コンテンツが発見されました。JSON:{0}", attachedContentJson));
                    break;
            }
            return content;
        }
        static JProperty GetContentBody(JArray attachedContentJson)
        {
            JObject json = (JObject)(
                attachedContentJson.Count > 7 && attachedContentJson[7].Type == JTokenType.Object ? attachedContentJson[7] :
                attachedContentJson.Count > 6 && attachedContentJson[6].Type == JTokenType.Object ? attachedContentJson[6] :
                attachedContentJson.Count > 5 && attachedContentJson[5].Type == JTokenType.Object ? attachedContentJson[5] :
                attachedContentJson.Count > 4 && attachedContentJson[4].Type == JTokenType.Object ? attachedContentJson[4] :
                attachedContentJson.Count > 2 && attachedContentJson[2].Type == JTokenType.Object ? attachedContentJson[2] :
                attachedContentJson.Count > 1 && attachedContentJson[1].Type == JTokenType.Object ? attachedContentJson[1] :
                null);
            if (json == null)
                throw new Exception("添付コンテンツの読み取りに予想外のデータが入ってきました。");
            var prop = json.Properties().First();
            return prop;
        }

        //Base
        static Uri ParseLinkUrl(JArray json, Uri plusBaseUrl)
        {
            string tmp;
            return string.IsNullOrEmpty(tmp = (string)json[0])
                ? null
                : tmp[0] == '/' ? new Uri(plusBaseUrl, tmp) : new Uri(tmp);
        }
        //Link
        static string ParseTitle(JArray json) { return (string)json[2]; }
        static string ParseSummary(JArray json)
        {
            string tmp;
            return string.IsNullOrEmpty(tmp = (string)json[3]) ? null : tmp;
        }
        static Uri ParseFaviconUrl(JArray json)
        {
            string tmp;
            return string.IsNullOrEmpty(tmp = (string)json[6])
                ? null : new Uri(ApiAccessorUtility.ComplementUrl(tmp, null));
        }
        static Uri ParseOriginalThumbnailUrl(JArray json)
        {
            var thumbJson = json[5].Type == JTokenType.Array ? (JArray)json[5] : null;
            if (thumbJson == null)
                return null;
            Uri tmp;
            return Uri.TryCreate((string)json[1], UriKind.Absolute, out tmp) ? tmp : null;
        }
        static string ParseThumbnailUrl(JArray json)
        {
            var thumbJson = json[5].Type == JTokenType.Array ? (JArray)json[5] : null;
            if (thumbJson == null)
                return null;
            return System.Text.RegularExpressions.Regex.Replace(
                ApiAccessorUtility.ComplementUrl((string)thumbJson[0], null), "w\\d+-h\\d+(-[^-]+)*", "$SIZE_SEGMENT");
        }
        static int ParseThumbnailWidth(JArray json)
        {
            var thumbJson = json[5].Type == JTokenType.Array ? (JArray)json[5] : null;
            if (thumbJson == null)
                return -1;
            return (int)thumbJson[1];
        }
        static int ParseThumbnailHeight(JArray json)
        {
            var thumbJson = json[5].Type == JTokenType.Array ? (JArray)json[5] : null;
            if (thumbJson == null)
                return -1;
            return (int)thumbJson[2];
        }
        //Album
        AttachedImageData[] ParsePictures(JArray json, Uri plusBaseUrl)
        { return json[41].Select(item => ((AttachedImageData)Generate((JArray)item, plusBaseUrl))).ToArray(); }
        static AlbumData ParseAlbum(JArray json, Uri plusBaseUrl)
        {
            //アルバム
            var albumTitle = (string)json[2];
            var albumId = (string)json[37];
            var ownerId = (string)json[26];
            return new AlbumData(albumId, albumTitle, ParseLinkUrl(json, plusBaseUrl), owner: new ProfileData(ownerId), loadedApiTypes: AlbumUpdateApiFlag.Base);
        }
        static ImageData ParseImage(JArray json, Uri plusBaseUrl)
        {
            return new ImageData(
                ImageUpdateApiFlag.Base, (string)json[38], (string)json[2], (int)json[20], (int)json[21],
                ApiAccessorUtility.ConvertReplasableUrl((string)json[1]), ParseLinkUrl(json, plusBaseUrl),
                owner: new ProfileData((string)json[26], loadedApiTypes: ProfileUpdateApiFlag.Unloaded));
        }
        //Interactive
        static string ParseProviderName(JArray json)
        {
            var workJson = (JArray)json[75];
            if (json[77].Type == JTokenType.Array)
            {
                workJson = (JArray)json[77];
                return (string)workJson[0];
            }
            else return null;
        }
        static Uri ParseProviderLogoUrl(JArray json)
        {
            var workJson = (JArray)json[75];
            if (json[77].Type == JTokenType.Array)
            {
                workJson = (JArray)json[77];
                return new Uri((string)workJson[2]);
            }
            else return null;
        }
        static Uri ParseActionUrl(JArray json)
        {
            var workJson = (JArray)json[75];
            return new Uri((string)workJson[0][3]);
        }
        static LabelType ParseLabel(JArray json)
        {
            var workJson = (JArray)json[75];
            LabelType tmp;
            var labelTypeStr = string.Join(string.Empty, ((string)workJson[2]).Split(' ').Select(str => str[0].ToString().ToUpper() + str.Substring(1)));
            if (Enum.TryParse(labelTypeStr, out tmp) == false)
                tmp = LabelType.Unknown;
            return tmp;
        }
        //YouTube
        static Uri ParseEmbedMovieUrl(JArray tmpJson)
        { return tmpJson[65].Type == JTokenType.String ? new Uri((string)tmpJson[65]) : null; }
    }
    public class NotificationDataFactory : DataFactory<NotificationData>
    {
        public NotificationDataFactory(IDataFactoryManager accessor) : base(accessor) { }
        public virtual NotificationData Generate(JToken source, Uri plusBaseUrl)
        {
            var id = (string)source[0];
            var title = (string)source[4][0][0][2];
            var summary = (string)source[4][0][0][3];
            var rawNoticedDate = ((ulong)source[9]).ToString();
            var noticedDate = ApiWrapper.GetDateTime((ulong)source[9] / 1000);
            var type = NotificationFlag.Unknown;
            foreach (string typeTxt in source[8])
                type |= ConvertFlags(typeTxt);

            NotificationData data;
            if (type.HasFlag(NotificationFlag.CircleAddBack)
                || type.HasFlag(NotificationFlag.CircleIn))
                data = new SocialNotificationData(
                    type, id, rawNoticedDate, title, summary,
                    ParseActors(source, plusBaseUrl, type), noticedDate);
            else if (type.HasFlag(NotificationFlag.DirectMessage)
                || type.HasFlag(NotificationFlag.Followup)
                || type.HasFlag(NotificationFlag.InviteCommunitiy)
                || type.HasFlag(NotificationFlag.Mension)
                || type.HasFlag(NotificationFlag.SubscriptionCommunitiy)
                || type.HasFlag(NotificationFlag.PlusOne)
                || type.HasFlag(NotificationFlag.Reshare)
                || type.HasFlag(NotificationFlag.Response))
                data = new ContentNotificationData(
                    type, id, rawNoticedDate, title, summary,
                    ParseActivity(source, plusBaseUrl, type),
                    ParseActors(source, plusBaseUrl, type), noticedDate);
            else if (type.HasFlag(NotificationFlag.CameraSyncUploaded)
                || type.HasFlag(NotificationFlag.TaggedImage)
                || type.HasFlag(NotificationFlag.NewPhotosAdded))
            {
                Uri albumLinkUrl;
                string[] imageUrls;
                ParsePhoto(source, plusBaseUrl, out albumLinkUrl, out imageUrls);
                data = new PhotoNotificationData(
                    type, id, rawNoticedDate, title, summary, albumLinkUrl, imageUrls, noticedDate);
            }
            else if (type.HasFlag(NotificationFlag.InviteHangout))
            {
                Uri hangoutLinkUrl;
                ProfileData hangoutInviter;
                ParseHangout(source, plusBaseUrl, out hangoutLinkUrl, out hangoutInviter);
                data = new HangoutNotificationData(
                    type, id, rawNoticedDate, title, summary, hangoutLinkUrl, hangoutInviter, noticedDate);
            }
            else
                data = new NotificationData(type, id, rawNoticedDate, title, summary, noticedDate);
            return data;
        }

        static NotificationItemData[] ParseActors(JToken source, Uri plusBaseUrl, NotificationFlag type)
        {
            var details = new List<NotificationItemData>();
            JToken tmpJson = source[4][1];
            if (((JArray)tmpJson[1]).Count == 0)
            {
                tmpJson = tmpJson[0][3];
                details.Add(new NotificationItemData(new ProfileData((string)tmpJson[1], (string)tmpJson[2], ApiAccessorUtility.ConvertReplasableUrl(
                    (string)tmpJson[0]), AccountStatus.Active, loadedApiTypes: ProfileUpdateApiFlag.Base), type, (string)source[4][0][1]));
            }
            else
            {
                foreach (var item in tmpJson[1]
                    .Select((item, idx) => new { Type = (string)source[5][idx][0], Detail = item }))
                {
                    tmpJson = item.Detail[0][1][0];
                    details.Add(new NotificationItemData(new ProfileData((string)tmpJson[1], (string)tmpJson[2], ApiAccessorUtility.ConvertReplasableUrl(
                        (string)tmpJson[0]), AccountStatus.Active, loadedApiTypes: ProfileUpdateApiFlag.Base), type, (string)item.Detail[1]));
                }
            }
            return details.ToArray();
        }
        static ActivityData ParseActivity(JToken source, Uri plusBaseUrl, NotificationFlag type)
        {
            //通知の発生地点となるActivityを抽出
            var activityJson = source[4][1][0];
            var activityId = (string)source[6].First(token => (int)token[0] == 1)[1];

            //コミュ新着と招待の通知の場合はActivity本体の情報がほとんど含まれない
            if (type.HasFlag(NotificationFlag.SubscriptionCommunitiy)
                || type.HasFlag(NotificationFlag.InviteCommunitiy))
                return new ActivityData(activityId);
            else
            {
                var activityText = (string)activityJson[1];
                var profileJson = activityJson[3];
                var activityActor = new ProfileData(
                    (string)profileJson[1], (string)profileJson[2],
                    ApiAccessorUtility.ConvertReplasableUrl((string)profileJson[0]),
                    loadedApiTypes: ProfileUpdateApiFlag.Base);
                return new ActivityData(
                    activityId, null, activityText, null, status: PostStatusType.First, owner: activityActor,
                    updaterTypes: ActivityUpdateApiFlag.Base);
            }
        }
        static void ParsePhoto(JToken source, Uri plusBaseUrl, out Uri linkUrl, out string[] imagesUrl)
        {
            var imgUrls = new List<string>();
            var tmp = source[4][1][0];
            var imgDatas = tmp[2];
            linkUrl = new Uri((string)tmp[4][0][0][2]);
            foreach (var item in imgDatas)
                imgUrls.Add(ApiAccessorUtility.ConvertReplasableUrl((string)item[0][0]));
            imagesUrl = imgUrls.ToArray();
        }
        static void ParseHangout(JToken source, Uri plusBaseUrl, out Uri linkUrl, out ProfileData actor)
        {
            var tmp = source[4][0];
            linkUrl = new Uri((string)tmp[2][2]);
            tmp = tmp[0][1][0];
            actor = new ProfileData(
                (string)tmp[1], (string)tmp[2], ApiAccessorUtility.ConvertReplasableUrl((string)tmp[0]),
                AccountStatus.Active, loadedApiTypes: ProfileUpdateApiFlag.Base);
        }
        static NotificationFlag ConvertFlags(string typeTxt)
        {
            NotificationFlag type;
            switch (typeTxt)
            {
                case "CIRCLE_PERSONAL_ADD":
                    type = NotificationFlag.CircleIn;
                    break;
                case "CIRCLE_RECIPROCATING_ADD":
                    type = NotificationFlag.CircleAddBack;
                    break;
                case "EVENTS_INVITE":
                    type = NotificationFlag.InviteEvent;
                    break;
                case "HANGOUT_INVITE":
                    type = NotificationFlag.InviteHangout;
                    break;
                case "STREAM_COMMENT_NEW":
                    type = NotificationFlag.Response;
                    break;
                case "STREAM_COMMENT_FOLLOWUP":
                    type = NotificationFlag.Followup;
                    break;
                case "STREAM_POST_AT_REPLY":
                case "STREAM_COMMENT_AT_REPLY":
                    type = NotificationFlag.Mension;
                    break;
                case "STREAM_PLUSONE_POST":
                case "STREAM_PLUSONE_COMMENT":
                    type = NotificationFlag.PlusOne;
                    break;
                case "STREAM_POST_SHARED":
                    type = NotificationFlag.DirectMessage;
                    break;
                case "STREAM_RESHARE":
                    type = NotificationFlag.Reshare;
                    break;
                case "SQUARE_SUBSCRIPTION":
                    type = NotificationFlag.SubscriptionCommunitiy;
                    break;
                case "SQUARE_INVITE":
                    type = NotificationFlag.InviteCommunitiy;
                    break;
                case "PHOTOS_CAMERASYNC_UPLOADED":
                    type = NotificationFlag.CameraSyncUploaded;
                    break;
                case "PHOTOS_NEW_PHOTO_ADDED":
                    type = NotificationFlag.NewPhotosAdded;
                    break;
                default:
                    type = NotificationFlag.Unknown;
                    break;
            }
            return type;
        }
    }

    public abstract class DataFactory<T>
    {
        public DataFactory(IDataFactoryManager manager) { Manager = manager; }
        public IDataFactoryManager Manager { get; private set; }
        public virtual void GetStubModeConfig(Dictionary<Expression<Func<T, object>>, object> config, string marker) { }
    }
    public interface IDataFactoryManager
    {
        ProfileDataFactory ProfileFactory { get; }
        ActivityDataFactory ActivityFactory { get; }
        CommentDataFactory CommentFactory { get; }
        AttachedDataFactory AttachedFactory { get; }
        NotificationDataFactory NotificationFactory { get; }
    }
}

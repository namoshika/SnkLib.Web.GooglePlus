using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    using SunokoLibrary.Web.GooglePlus.Primitive;
    using SunokoLibrary.Threading;

    public class DefaultAccessor : IApiAccessor
    {
        public async Task<IPlatformClientBuilder[]> GetAccountList(CookieContainer cookies)
        {
            try
            {
                var client = new System.Net.Http.HttpClient(new System.Net.Http.HttpClientHandler() { CookieContainer = cookies });
                client.DefaultRequestHeaders.Add("user-agent", ApiAccessorUtility.UserAgent);
                var json = JArray.Parse(await ApiWrapper.LoadListAccounts(client));
                var generators = json[1]
                    .Select(item => new PlatformClientBuilder((string)item[3], (string)item[2], ApiAccessorUtility.ConvertReplasableUrl((string)item[4]), (int)item[7], cookies, this))
                    .ToArray();
                return generators;
            }
            catch (System.Net.Http.HttpRequestException e)
            { throw new FailToOperationException("引数cookiesからログインされているアカウントの取得に失敗しました。", e); }
        }
        [Obsolete("このメソッドを使用すべきではありません。認証を使わずに外部からのCookieの取り込みを検討してください。")]
        public Task<bool> LoginAsync(string email, string password, IPlatformClient client)
        { return ApiWrapper.ConnectToServiceLoginAuth(client.NormalHttpClient, client.PlusBaseUrl, client.Cookies, email, password); }
        public async Task<InitData> GetInitDataAsync(IPlatformClient client)
        {
            try
            {
                var hmIntDt = await Primitive.ApiWrapper.LoadHomeInitData(client.NormalHttpClient, client.PlusBaseUrl);
                var atVal = (string)hmIntDt[1][15];
                var pvtVal = (string)hmIntDt[1][28];
                var eJxVal = (string)hmIntDt[161][1][1];
                var circleInfos = hmIntDt[12][0]
                    .Select(item => new CircleData((string)item[0][0], (string)item[1][0], null))
                    .ToArray();
                var latestActivities = hmIntDt[161][1][7]
                    .Where(item => (string)item[0] == "1002")
                    .Select(jsonItem => GenerateActivityData(jsonItem[1]["33558957"], ActivityUpdateApiFlag.GetActivities, client))
                    .ToArray();
                return new InitData(atVal, pvtVal, eJxVal, circleInfos, latestActivities); ;
            }
            catch (KeyNotFoundException e)
            { throw new ApiErrorException("トップページのパラメータ取得に失敗。ログインセッションが失効しています。", ErrorType.SessionError, new Uri("https://plus.google.com"), null, null, e); }
        }
        public async Task<Tuple<CircleData[], ProfileData[]>> GetCircleDatasAsync(IPlatformClient client)
        {
            var lookupedProfiles = new List<ProfileData>();
            var circles = new Dictionary<string, Tuple<string, List<ProfileData>>>();
            var json = JToken.Parse(await Primitive.ApiWrapper.ConnectToLookupCircles(client.NormalHttpClient, client.PlusBaseUrl, client.AtValue));
            var lastUpdateDate = DateTime.UtcNow;

            //サークル一覧生成。List<ProfileInfo>の初期化と一緒にサークル名も
            //この時点で取得してしまう。
            foreach (var item in json[0][1][1])
                circles.Add((string)item[0][0], Tuple.Create((string)item[1][0], new List<ProfileData>()));
            //profileIdをサークルに振り分けてく
            foreach (var item in json[0][1][2])
            {
                var profileId = (string)item[0].ElementAtOrDefault(2) ?? (string)item[0][0];
                var profile = GenerateProfileData(item, lastUpdateDate, ProfileUpdateApiFlag.LookupCircle);
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
            var resProfiles = new List<ProfileData>(json[0][1][2]
                .Select(item => GenerateProfileData(item, lastUpdateDate, ProfileUpdateApiFlag.LookupCircle)));

            return Tuple.Create(resCircles.ToArray(), resProfiles.ToArray());
        }
        public async Task<ProfileData> GetProfileLiteAsync(string profileId, IPlatformClient client)
        {
            var json = JToken.Parse(await Primitive.ApiWrapper.ConnectToLookupPeople(client.NormalHttpClient, client.PlusBaseUrl, profileId, client.AtValue));
            var lastUpdateDate = DateTime.UtcNow;
            return GenerateProfileData(json[0][1][2][0], lastUpdateDate, ProfileUpdateApiFlag.LookupProfile);
        }
        public async Task<ProfileData> GetProfileFullAsync(string profileId, IPlatformClient client)
        {
            var apiResponse = JToken.Parse(await ApiWrapper.ConnectToProfileGet(client.NormalHttpClient, client.PlusBaseUrl, profileId));
            var lastUpdateDate = DateTime.UtcNow;
            return GenerateProfileData(apiResponse[0][1][1][2], lastUpdateDate, ProfileUpdateApiFlag.ProfileGet);
        }
        public async Task<ProfileData> GetProfileAboutMeAsync(IPlatformClient client)
        {
            var json = JToken.Parse(await ApiWrapper.ConnectToGetIdentities(client.NormalHttpClient, client.PlusBaseUrl));
            var id = (string)json[0][0][1];
            var lastUpdateDate = DateTime.UtcNow;
            if (id == null)
                throw new ApiErrorException("自身のPlusID取得に失敗しました。ログインされていない可能性があります。", ErrorType.SessionError, null, null, null, null);
            return GenerateProfileData(json[0][1][1][0], lastUpdateDate, ProfileUpdateApiFlag.ProfileGet);
        }
        public async Task<ProfileData[]> GetFollowingProfilesAsync(string profileId, IPlatformClient client)
        {
            var results = new List<ProfileData>();
            var json = JToken.Parse(await ApiWrapper.ConnectToLookupVisible(client.NormalHttpClient, client.PlusBaseUrl, profileId, client.AtValue));
            foreach (var item in json[0][1][2])
                results.Add(new ProfileData(
                    (string)item[0][2], (string)item[2][0], ApiAccessorUtility.ConvertReplasableUrl((string)item[2][8]),
                    greetingText: (string)item[2][21], loadedApiTypes: ProfileUpdateApiFlag.Base));
            return results.ToArray();
        }
        public async Task<ProfileData[]> GetFollowedProfilesAsync(string profileId, int count, IPlatformClient client)
        {
            var results = new List<ProfileData>();
            var json = JToken.Parse(await ApiWrapper.ConnectToLookupIncoming(client.NormalHttpClient, client.PlusBaseUrl, profileId, count, client.AtValue));
            foreach (var item in json[0][1][2])
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
            var json = JToken.Parse(await Primitive.ApiWrapper.ConnectToLookupIgnore(client.NormalHttpClient, client.PlusBaseUrl, client.AtValue));
            var ignoreLst = new List<ProfileData>();
            foreach (var item in json[0][1][2])
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
            var json = JToken.Parse(await Primitive.ApiWrapper.ConnectToLookupFollowers(
                client.NormalHttpClient, client.PlusBaseUrl, client.AtValue))[0][1];
            var followersLst = new List<ProfileData>();
            foreach (var jsonB in json.Skip(1).Take(2))
                foreach (var item in jsonB)
                {
                    var profileId = (string)item[0].ElementAtOrDefault(2);
                    var iconUrl = ApiAccessorUtility.ConvertReplasableUrl((string)item[2][8]);
                    followersLst.Add(new ProfileData(
                        profileId, (string)item[2][0], iconUrl, loadedApiTypes: ProfileUpdateApiFlag.Base));
                }
            return followersLst.ToArray();
        }
        public async Task<ProfileData[]> GetProfileOfPusherAsync(string plusOneId, int pushCount, IPlatformClient client)
        {
            var resLst = new List<ProfileData>();
            var json = JToken.Parse(await ApiWrapper.ConnectToCommonGetPeople(
                client.NormalHttpClient, client.PlusBaseUrl, plusOneId, pushCount, client.AtValue))[0][1][1];
            foreach (var item in json)
                resLst.Add(new ProfileData(
                    (string)item[1], (string)item[0], ApiAccessorUtility.ConvertReplasableUrl((string)item[3]),
                    status: AccountStatus.Active, loadedApiTypes: ProfileUpdateApiFlag.Base));
            return resLst.ToArray();
        }
        public async Task<ActivityData> GetActivityAsync(string activityId, IPlatformClient client)
        {
            return GenerateActivityData(
                JToken.Parse(await ApiWrapper.ConnectToGetActivity(client.NormalHttpClient, client.PlusBaseUrl, activityId))[0][1],
                ActivityUpdateApiFlag.GetActivity, client);
        }
        public async Task<Tuple<ActivityData[], string>> GetActivitiesAsync(string circleId, string profileId, string ctValue, int length, IPlatformClient client)
        {
            var activities = new List<ActivityData>();
            var nowDate = DateTime.UtcNow;
            for (var i = 0; i < length; )
            {
                var oneSetSize = Math.Min(length, 40);
                var oneSetCount = 0;
                var apiResult = JToken.Parse(await Primitive.ApiWrapper.ConnectToGetActivities(
                    client.NormalHttpClient, client.PlusBaseUrl, client.AtValue, oneSetSize, circleId, profileId, ctValue))[0][1][1];
                ctValue = (string)apiResult[1];
                foreach (var item in apiResult[0])
                {
                    oneSetCount++;
                    activities.Add(GenerateActivityData(item, ActivityUpdateApiFlag.GetActivities, client));
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
                var apiResponse = JToken.Parse(await ApiWrapper.ConnectToNotificationsFetch(
                    client.NormalHttpClient, client.PlusBaseUrl, isFetchNewItemMode, client.AtValue,
                    Math.Min(length, MAX_RESULT_LENGTH), continueToken))[0][1];
                continueToken = apiResponse[2].Type == JTokenType.Array ? (string)apiResponse[2][5] : null;
                if (latestReadedItemNotifiedDate == DateTime.MinValue)
                    latestReadedItemNotifiedDate = ApiWrapper.GetDateTime((ulong)apiResponse[4] / 1000);
                foreach (var item in apiResponse[1])
                {
                    try
                    {
                        var data = NotificationData.Create(item, client.PlusBaseUrl);
                        notificationList.Add(data);
                        length--;
                    }
                    //処理の特性上、変なデータには何時でも遭遇し得る。
                    //そのため、想定外のデータとの遭遇は想定するエラーとして黙殺する。
                    catch (InvalidDataException)
                    {
                        if (System.Diagnostics.Debugger.IsAttached)
                            System.Diagnostics.Debugger.Break();
                        System.Diagnostics.Debug.WriteLine("未知のデータを取得: {0}", item);
                        continue;
                    }
                }
            }
            while (length > 0 && continueToken != null);
            return Tuple.Create(notificationList.ToArray(), latestReadedItemNotifiedDate, continueToken);
        }
        public async Task<int> GetUnreadNotificationCountAsync(IPlatformClient client)
        {
            var json = JToken.Parse(await Primitive.ApiWrapper.ConnectToGsuc(client.NormalHttpClient, client.PlusBaseUrl));
            return (int)json[0];
        }
        public async Task<AlbumData> GetAlbumAsync(string albumId, string profileId, IPlatformClient client)
        {
            var json = JToken.Parse(await ApiWrapper.ConnectToPhotosAlbums(client.NormalHttpClient, client.PlusBaseUrl, profileId, albumId));
            return GenerateAlbumData(json[0][1][1], json[0][1][2], AlbumUpdateApiFlag.Full, client.PlusBaseUrl);
        }
        public async Task<AlbumData[]> GetAlbumsAsync(string profileId, IPlatformClient client)
        {
            var json = JToken.Parse(await ApiWrapper.ConnectToPhotosAlbums(client.NormalHttpClient, client.PlusBaseUrl, profileId));
            var resultAlbums = new List<AlbumData>();
            foreach (var item in json[0][1][2])
                resultAlbums.Add(GenerateAlbumData(item, null, AlbumUpdateApiFlag.Base, client.PlusBaseUrl));
            return resultAlbums.ToArray();
        }
        public async Task<ImageData> GetImageAsync(string imageId, string profileId, IPlatformClient client)
        {
            var json = JToken.Parse(await ApiWrapper.ConnectToPhotosLightbox(client.NormalHttpClient, client.PlusBaseUrl, profileId, imageId))[1][1];
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
            var isolateActivity = GenerateActivityData(json[10], ActivityUpdateApiFlag.GetActivity, client);
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
            return Primitive.ApiWrapper
                .ConnectToTalkGadgetBind(client.NormalHttpClient, client.StreamHttpClient, client.TalkBaseUrl, client.Cookies, client.PvtValue)
                .Where(json => (string)json[1][0] == "c")
                .Select(json => GenerateDataFromStreamingApi(json, client));
        }
        public async Task MarkAsReadAsync(NotificationData target, IPlatformClient client)
        {
            await ApiWrapper.ConnectToSetReadStates(
                client.NormalHttpClient, client.PlusBaseUrl, target.Id, target.RawNoticedDate, client.AtValue);
            if (target is ContentNotificationData)
                await ApiWrapper.ConnectToMarkItemRead(
                    client.NormalHttpClient, client.PlusBaseUrl,
                    ((ContentNotificationData)target).Target.Id, client.AtValue);
        }
        public async Task<CommentData> PostComment(string activityId, string content, IPlatformClient client)
        {
            var apiResponse = JToken.Parse(
                await ApiWrapper.ConnectToComment(client.NormalHttpClient, client.PlusBaseUrl, activityId, content, DateTime.Now, client.AtValue));
            return GenerateCommentData(apiResponse[0][1][1]);
        }
        public async Task<CommentData> EditComment(string activityId, string commentId, string content, IPlatformClient client)
        {
            var apiResponse = JToken.Parse(
                await ApiWrapper.ConnectToEditComment(client.NormalHttpClient, client.PlusBaseUrl, activityId, commentId, content, client.AtValue));
            return GenerateCommentData(apiResponse[0][1][1]);
        }
        public Task DeleteComment(string commentId, IPlatformClient client)
        { return ApiWrapper.ConnectToDeleteComment(client.NormalHttpClient, client.PlusBaseUrl, commentId, client.AtValue); }

        static ProfileData GenerateProfileData(JToken apiResponse, DateTime? lastUpdateDate, ProfileUpdateApiFlag apiType)
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
                        JToken tmpJsonA, tmpJsonB;
                        tmpJsonA = apiResponse[4];
                        var id = (string)apiResponse[30];
                        var name = (string)tmpJsonA[3];
                        var firstName = (string)tmpJsonA[1];
                        var lastName = (string)tmpJsonA[2];
                        var introduction = (string)apiResponse[14][1];
                        tmpJsonB = apiResponse[19];
                        var braggingRights = (string)tmpJsonB.ElementAtOrDefault(1) ?? string.Empty;
                        tmpJsonB = apiResponse[6];
                        var occupation = (string)tmpJsonB.ElementAtOrDefault(1) ?? string.Empty;
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
                        tmpJsonB = apiResponse[22];
                        var relationship = (RelationType)(int.Parse((string)tmpJsonB.ElementAtOrDefault(1) ?? "-1"));
                        var lookingFor = new LookingFor(apiResponse[23]);
                        var greetingText = (string)apiResponse[33][1];
                        tmpJsonB = apiResponse[17];
                        var genderType = (GenderType)(int.Parse((string)tmpJsonB.ElementAtOrDefault(1) ?? "-1"));
                        var otherNames = apiResponse[5][1].Select(token => (string)token[0]).ToArray();
                        tmpJsonB = apiResponse[47];
                        var nickName = (string)tmpJsonB.ElementAtOrDefault(1) ?? string.Empty;

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
        static ActivityData GenerateActivityData(JToken apiResponse, ActivityUpdateApiFlag loadedApiTypes, IPlatformClient client)
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
            var comments = apiResponse[7].Select(itm => GenerateCommentData(itm)).ToArray();

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
                ? AttachedBase.Create((JArray)apiResponse[97], client.PlusBaseUrl) : null;
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
                    (string)apiResponse[40], attachedHtml, attachedText, attachedElement,
                    (string)apiResponse[44][1], (string)apiResponse[44][0],
                    ApiAccessorUtility.ConvertReplasableUrl((string)apiResponse[44][4]),
                    new Uri(client.PlusBaseUrl, (string)apiResponse[77]),
                    atchCnt);
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
        static CommentData GenerateCommentData(JToken apiResponse)
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
        static object GenerateDataFromStreamingApi(JToken rawItem, IPlatformClient client)
        {
            if (rawItem[1][1].Type != JTokenType.Array)
                return rawItem;
            var json = rawItem[1][1][1];
            switch ((string)json[0])
            {
                case "tu":
                    var shrItm = JArray.Parse(Primitive.ApiWrapper.ConvertIntoValidJson((string)json[1]));
                    switch ((string)shrItm[0])
                    {
                        case "t.rtc":
                            return GenerateCommentData(shrItm[1]);
                        case "t.rtu":
                            return GenerateActivityData(shrItm[1], ActivityUpdateApiFlag.GetActivities, client);
                        case "t.rtd":
                            {
                                if (shrItm.Count >= 3)
                                {
                                    var cid = (string)shrItm[2];
                                    var aid = cid.Substring(0, cid.LastIndexOf('#'));
                                    return (object)new CommentData(cid, aid, null, DateTime.MinValue, DateTime.MinValue, null, PostStatusType.Removed);
                                }
                                else
                                    return (object)new ActivityData((string)shrItm[1], status: PostStatusType.Removed, updaterTypes: ActivityUpdateApiFlag.GetActivities);
                            }
                        default:
                            System.Diagnostics.Debug.Assert(false, "talkgadgetBindに想定外のjsonが入ってきました。");
                            return rawItem;
                    }
                case "gb":
                    var notificationItem = JArray.Parse(
                        Primitive.ApiWrapper.ConvertIntoValidJson((string)json[1]));
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
}

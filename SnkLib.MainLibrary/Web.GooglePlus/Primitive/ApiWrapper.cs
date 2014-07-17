using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Reactive;
using System.Reactive.Linq;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    using Newtonsoft.Json.Linq;
    using System.Xml.Linq;

    public class ApiWrapper
    {
        public static readonly ApiWrapper Default = new ApiWrapper();
        static readonly DateTime DateUnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        [Obsolete("このメソッドはGoogleサーバー側に不正アクセスと誤認識される事があるため、使用すべきではありません。")]
        public async Task<bool> ConnectToServiceLoginAuth(HttpClient client, Uri plusBaseUrl, System.Net.CookieContainer responseCheckTarget, string email, string password)
        {
            const string loginPageUrl = "https://accounts.google.com/ServiceLogin";
            const string authPageUrl = "https://accounts.google.com/ServiceLoginAuth";
            const string htmlInputElementPattern = "<input[^>]+name=[\"'](?<key>[^\"']*)[\"'][^>]+value=[\"'](?<value>[^\"']*)[\"'][^>]*>";

            //必要な値をログインページから取ってくる
            var loginPageHtm = await GetStringAsync(client, new Uri(string.Format("{0}?service=oz&continue={1}&hl=ja", loginPageUrl, plusBaseUrl)));

            //想定されたレスポンスを返したかチェック
            var cookie = responseCheckTarget.GetCookies(new Uri(loginPageUrl));
            if (cookie["GALX"] == null || cookie["GAPS"] == null || cookie["GoogleAccountsLocale_session"] == null)
                throw new Exception("ログインの仕組みが新しいものに変更されているため、このライブラリではログイン処理を進めることができません。");

            //認証用クエリ生成
            var queryCollection = new Dictionary<string, string>();
            queryCollection.Add("Email", email);
            queryCollection.Add("Passwd", password);
            var match = System.Text.RegularExpressions.Regex.Match(loginPageHtm, htmlInputElementPattern);
            while (match.Success)
            {
                var key = match.Groups["key"].Value;
                if (string.IsNullOrEmpty(key) == false && key != "Email" && key != "Passwd")
                {
                    var val = match.Groups["value"].Value;
                    queryCollection.Add(key, val);
                }
                match = match.NextMatch();
            }
            System.Diagnostics.Debug.Assert(
                queryCollection.Count == 16, "ログインフォームのパラメータに変化があります。ログイン処理の変化を確認してください。");

            //認証開始
            var isFail = true;
            using (var res = await client.PostAsync(new Uri(authPageUrl), new FormUrlEncodedContent(queryCollection)))
                if (res.IsSuccessStatusCode)
                {
                    //想定されたレスポンスを返したかチェック
                    //accountドメインのcookieをチェック
                    cookie = responseCheckTarget.GetCookies(new Uri(loginPageUrl));
                    isFail = cookie["SID"] == null || cookie["LSID"] == null || cookie["HSID"] == null
                        || cookie["SSID"] == null || cookie["APISID"] == null || cookie["SAPISID"] == null;
                    //plusドメインのcookieをチェック
                    cookie = responseCheckTarget.GetCookies(plusBaseUrl);
                    isFail |= cookie["SID"] == null;

                    System.Diagnostics.Debug.Assert(isFail == false,
                        "ログインの仕組みが新しいものに変更されているため、このライブラリではログイン処理を進めることができません。");
                }
            return isFail == false;
        }
        public async Task<string> ConnectToInitialData(HttpClient client, Uri plusBaseUrl, int key, string atVal)
        {
            var url = new Uri(plusBaseUrl, string.Format("_/initialdata?key={0}&rt=j", key));
            var jsonTxt = await PostStringAsync(client, url, new Dictionary<string, string>() { { "at", atVal }, });
            return jsonTxt;
        }
        public async Task<string> ConnectToGetIdentities(HttpClient client, Uri plusBaseUrl)
        {
            //api error: innerEx == null
            //ses error: innerEx is WebException
            var url = new Uri(plusBaseUrl, "_/pages/getidentities/?hl=ja&rt=j");
            var jsonTxt = await GetStringAsync(client, url);
            return jsonTxt;
        }
        public async Task<string> ConnectToProfileGet(HttpClient client, Uri plusBaseUrl, string plusId)
        {
            var url = new Uri(plusBaseUrl, string.Format("_/profiles/get/{0}/posts?hl=ja&rt=j", plusId));
            var jsonTxt = await GetStringAsync(client, url);
            return jsonTxt;
        }
        public async Task<string> ConnectToLookupPeople(HttpClient client, Uri plusBaseUrl, string[] plusIds, string atVal)
        {
            var jsonTxt = await PostStringAsync(
                client,
                new Uri(plusBaseUrl, "_/socialgraph/lookup/people/?if=true&rt=j"),
                new Dictionary<string, string>()
                    {
                        { "m", "[[" + string.Join(",", plusIds.Select(id => string.Format("[null,null,\"{0}\"]", id))) + "]]" },
                        { "at", atVal },
                    });
            return jsonTxt;
        }
        public async Task<string> ConnectToLookupCircles(HttpClient client, Uri plusBaseUrl, string atValue)
        {
            var circlesUrl = new Uri(plusBaseUrl, "_/socialgraph/lookup/circles/?ct=2&m=true&rt=j");
            var jsonTxt = await PostStringAsync(client, circlesUrl, new Dictionary<string, string>() { { "at", atValue } });
            return jsonTxt;
        }
        public async Task<string> ConnectToLookupFollowers(HttpClient client, Uri plusBaseUrl, string ozBuildLevel, string ozSid, string ozLang, string atValue)
        {
            ;
            var url = new Uri(plusBaseUrl, string.Format("_/people/haveyou?{0}", await MakeQuery(new Dictionary<string, string>()
                {
                    { "soc-app", "1" },
                    { "cid", "0" },
                    { "soc-platform", "1" },
                    { "hl", ozLang },
                    { "ozv", ozBuildLevel },
                    { "avw", "pv:1" },
                    { "f.sid", ozSid },
                    { "_reqid", "953042" },
                    { "rt","j" }
                })));
            var jsonTxt = await PostStringAsync(client, url, new Dictionary<string, string>()
                {
                    { "at", atValue },
                    { "f.req", "[\"ppv.dr\",[[\"ppv.drp\",9]]]" },
                });
            return jsonTxt;
        }
        public async Task<string> ConnectToLookupIgnore(HttpClient client, Uri plusBaseUrl, string atValue)
        {
            var ignoreUrl = new Uri(plusBaseUrl, "_/socialgraph/lookup/ignored/?m=5000&rt=j");
            var jsonTxt = await PostStringAsync(client, ignoreUrl, new Dictionary<string, string>() { { "at", atValue } });
            return jsonTxt;
        }
        public async Task<string> ConnectToLookupVisible(HttpClient client, Uri plusBaseUrl, string plusId, string atValue)
        {
            var jsonTxt = await PostStringAsync(
                client,
                new Uri(plusBaseUrl, "_/socialgraph/lookup/visible/"),
                new Dictionary<string, string>()
                    {
                        { "o", string.Format("[null,null,\"{0}\"]", plusId) },
                        { "rt", "j" },
                        { "at", atValue },
                    });
            return jsonTxt;
        }
        public async Task<string> ConnectToLookupIncoming(HttpClient client, Uri plusBaseUrl, string plusId, int count, string atValue)
        {
            var jsonTxt = await PostStringAsync(
                client,
                new Uri(plusBaseUrl, "_/socialgraph/lookup/incoming/"),
                new Dictionary<string, string>()
                    {
                        { "o", string.Format("[null,null,\"{0}\"]", plusId) },
                        { "s", "true" },
                        { "n", count.ToString() },
                        { "rt", "j" },
                        { "at", atValue },
                    });
            return jsonTxt;
        }
        public async Task<string> ConnectToGetActivities(HttpClient client, Uri plusBaseUrl, string atVal, int length = 20, string circleId = null, string plusId = null, string ct = null)
        {
            //query作成
            var query = new Dictionary<string, string>()
                {
                    { "hl", "ja" },
                    {
                        "f.req",
                        string.Format(
                            "[[1,2,{0},{1},null,{2},null,\"social.google.com\",[],null,null,null,null,null,null,[]]{3}]",
                            plusId == null ? "null" : string.Format("\"{0}\"", plusId),
                            circleId == null ? "null" : string.Format("\"{0}\"", circleId),
                            length,
                            ct != null ? string.Format(",\"{0}\"", ct) : string.Empty
                        )
                    },
                    { "at", atVal }
                };

            //download
            var resStr = await PostStringAsync(client, new Uri(plusBaseUrl, "_/stream/getactivities/?rt=j"), query);
            return resStr;
        }
        public async Task<string> ConnectToGetActivity(HttpClient client, Uri plusBaseUrl, string id, string ozLang, string ozBuildLabel, string ozSid)
        {
            var resStr = await GetStringAsync(client, new Uri(plusBaseUrl, string.Format("_/stream/getactivity/?{0}", await MakeQuery(new Dictionary<string,string>()
                {
                    { "updateId", id },
                    { "hl", ozLang },
                    { "ozv", ozBuildLabel },
                    { "avw", "str:1" },
                    { "f.sid", ozSid },
                    { "rt", "j" },
                }))));
            return resStr;
        }
        public async Task<string> ConnectToPost(HttpClient client, Uri plusBaseUrl, DateTime postDate, int postCount, string plusId, Dictionary<string, string> targetCircles, Dictionary<string, string> targetUsers, ContentType? attachedContentType, string content, bool isDisabledComment, bool isDisabledReshare, string atVal)
        {
            var postTime = string.Format("{0:X}", GetUnixTime(postDate)).ToLower();
            var postRange = new
            {
                aclEntries = Enumerable.Concat(
                    targetCircles
                        .SelectMany(pair => new[] {
                            (object)new { scope = new { scopeType = pair.Key == "anyone" ? "anyone" : "focusGroup", name = pair.Value, id = string.Format("{0}.{1}", plusId, pair.Key), me = false, requiresKey = false, groupType = "p" }, role = 20},
                            (object)new { scope = new { scopeType = pair.Key == "anyone" ? "anyone" : "focusGroup", name = pair.Value, id = string.Format("{0}.{1}", plusId, pair.Key), me = false, requiresKey = false, groupType = "p" }, role = 60}
                        }),
                    targetUsers
                        .SelectMany(pair => new[] {
                            (object)new { scope = new { scopeType = "user", name = pair.Value, id = pair.Key, me = false, requiresKey = false, isMe = false }, role = 20 },
                            (object)new { scope = new { scopeType = "user", name = pair.Value, id = pair.Key, me = false, requiresKey = false, isMe = false }, role = 60 }
                        }))
            };

            // 画像投稿だったらここに
            string imagePostParameter;
            switch (attachedContentType)
            {
                case ContentType.Image:
                //case ContentType.Album:
                //case ContentType.Video:
                case ContentType.YouTube:
                    imagePostParameter = "\"" + plusId + "\"";
                    break;
                default:
                    imagePostParameter = null;
                    break;
            }
            // メディアレイアウトかどうか
            bool isMediaLayout;
            switch (attachedContentType)
            {
                case ContentType.Image:
                    //case ContentType.InfoType.Album:
                    //case ContentType.InfoType.Video:
                    //case ContentType.InfoType.YouTube:
                    isMediaLayout = true;
                    break;
                default:
                    isMediaLayout = false;
                    break;
            }

            // 検索結果投稿の場合これが必要
            string searchResultParameter = null;
            //if (attachedSearchResult != null)
            //{
            //    searchResultParameter = attachedSearchResult.AttachedParameter;
            //}
            // ロケーション情報がある場合
            string locationInfoParameter = null;
            //if (attachedLocation != null)
            //{
            //    attachedLocation.WaitSetLocationCompleted(isDoEvent);
            //    locationInfoParameter = attachedLocation.AttachedParameter;
            //}
            var sparObj = new object[]
            {
                content, string.Format("oz:{0}.{1}.{2}", plusId, postTime, postCount), null,null,
                null,null, "[]", locationInfoParameter, Newtonsoft.Json.JsonConvert.SerializeObject(postRange),
                true, new object[]{ }, false, false, null, new object[]{ }, false, false, null, null,
                imagePostParameter, null, searchResultParameter, null, null, null, null, null,
                isDisabledComment, isDisabledReshare, isMediaLayout, null,null,null,null,null,null,
                new object[]{ }
            };
            var sparJson = Newtonsoft.Json.JsonConvert.SerializeObject(sparObj);
            var parameter = new Dictionary<string, string>()
                {
                    { "spar", (string)sparJson },
                    { "at", atVal }
                };
            var url = new Uri(plusBaseUrl, "_/sharebox/post/?spam=20&rt=j");
            var jsonTxt = await PostStringAsync(client, url, parameter);
            return jsonTxt;
        }
        public async Task<string> ConnectToComment(HttpClient client, Uri plusBaseUrl, string activityId, string content, DateTime postDate, string atVal)
        {
            var url = new Uri(plusBaseUrl, "_/stream/comment/?rt=j");
            var prmsStr = new Dictionary<string, string>()
                {
                    { "itemId", activityId },
                    { "clientId", string.Format("os:{0}:{1}", activityId, postDate) },
                    { "text", content },
                    { "timestamp_msec", GetUnixTime(postDate).ToString() },
                    { "at", atVal },
                };
            var jsonTxt = await PostStringAsync(client, url, prmsStr);
            return jsonTxt;
        }
        public async Task<string> ConnectToEditComment(HttpClient client, Uri plusBaseUrl, string activityId, string commentId, string content, string atVal)
        {
            var url = new Uri(plusBaseUrl, "_/stream/editcomment/?rt=j");
            var query = new Dictionary<string, string>()
                {
                    { "itemId", activityId },
                    { "commentId", commentId },
                    { "text", content },
                    { "at", atVal },
                };
            var jsonTxt = await PostStringAsync(client, url, query);
            return jsonTxt;
        }
        public async Task<string> ConnectToDeleteComment(HttpClient client, Uri plusBaseUrl, string commentId, string atVal)
        {
            var url = new Uri(plusBaseUrl, "_/stream/deletecomment/?rt=j");
            var query = new Dictionary<string, string>()
                {
                    { "commentId", commentId },
                    { "at", atVal },
                };
            var jsonTxt = await PostStringAsync(client, url, query);
            return jsonTxt;
        }
        public async Task<string> ConnectToPlusOne(HttpClient client, Uri plusBaseUrl, string targetId, bool isPlusOned, string atVal)
        {
            var url = new Uri(plusBaseUrl, "_/plusone?rt=j");
            var query = new Dictionary<string, string>()
                {
                    { "itemId", targetId },
                    { "set", isPlusOned ? "true" : "false" },
                    { "at", atVal },
                };
            var jsonTxt = await PostStringAsync(client, url, query);
            return jsonTxt;
        }
        public async Task<string> ConnectToCommonGetPeople(HttpClient client, Uri plusBaseUrl, string plusoneId, int length, string atVal)
        {
            var url = new Uri(plusBaseUrl, "_/common/getpeople/?rt=j");
            var query = new Dictionary<string, string>()
                {
                    { "plusoneId", plusoneId },
                    { "num", length.ToString() },
                    { "hl", "ja" },
                    { "at", atVal },
                };
            var jsonTxt = await PostStringAsync(client, url, query);
            return jsonTxt;
        }
        public async Task<string> ConnectToNotificationsData(HttpClient client, Uri plusBaseUrl, string atVal, NotificationsFilter type = NotificationsFilter.All, int maxResults = 15, string continueToken = null)
        {
            var queryArray = new Dictionary<string, string>()
                {
                    { "soc-app", "1" },
                    { "cid", "0" },
                    { "soc-platform", "1" },
                    { "hl", "ja" },
                    { "avw", "str:1" },
                    { "rt","j" }
                };
            var paramArray = new Dictionary<string, string>()
                {
                    { "f.req", string.Format("[{0},[],6,null,[],null,null,[],null,{1},{2},null,null,null,null,null,[18],null,null,2]", type == NotificationsFilter.All ? "null" : ((int)type).ToString(), continueToken != null ? "\"" + continueToken + "\"" : "null", maxResults) },
                    { "at", atVal }
                };
            var query = await MakeQuery(queryArray);
            var jsonTxt = await PostStringAsync(client, new Uri(plusBaseUrl, "_/notifications/getnotificationsdata?" + query), paramArray);
            return jsonTxt;
        }
        public async Task<string> ConnectToNotificationsFetch(HttpClient client, Uri plusBaseUrl, bool isFetchNewItemMode, string atVal, int maxResults = 15, string continueToken = null)
        {
            var queryDict = new Dictionary<string, string>()
                {
                    { "soc-app", "1" },
                    { "cid", "0" },
                    { "soc-platform", "1" },
                    { "hl", "ja" },
                    { "avw", "str:1" },
                    { "rt","j" },
                };
            var paramDict = new Dictionary<string, string>()
                {
                    { "f.req", string.Format("[[\"OGB\",[7]],[null,null,{0},[],[{1}],{2},\"GPLUS_APP\",[3]],[3]]", maxResults, isFetchNewItemMode ? "1" : "2", continueToken != null ? string.Format("\"{0}\"", continueToken) : "null") },
                    { "at", atVal }
                };
            var jsonTxt = await PostStringAsync(client, new Uri(plusBaseUrl, "_/notifications/fetch?" + await MakeQuery(queryDict)), paramDict);
            return jsonTxt;
        }
        public async Task<string> ConnectToMarkItemRead(HttpClient client, Uri plusBaseUrl, string activityIds, string atValue)
        {
            var queryDict = new Dictionary<string, string>()
                {
                    { "hl", "ja" },
                    { "avw", "str:1" },
                    { "rt","j" },
                };
            var paramDict = new Dictionary<string, string>()
                {
                    { "itemIds", activityIds },
                    { "netType", "4" },
                    { "at", atValue }
                };
            var jsonTxt = await PostStringAsync(client, new Uri(plusBaseUrl, "_/stream/markitemread/?" + await MakeQuery(queryDict)), paramDict);
            return jsonTxt;
        }
        public async Task<string> ConnectToSetReadStates(HttpClient client, Uri plusBaseUrl, string notificationIds, string rawNoticedDate, string atValue)
        {
            var queryDict = new Dictionary<string, string>()
                {
                    { "soc-app", "1" },
                    { "cid", "0" },
                    { "soc-platform","1" },
                    { "rt","j" },
                };
            var paramDict = new Dictionary<string, string>()
                {
                    {"f.req", string.Format("[[\"OGB\",[7]],[[[\"{0}\",null,\"{1}\"]],2]]", notificationIds, rawNoticedDate) },
                    {"at", atValue }
                };
            var jsonTxt = await PostStringAsync(client, new Uri(plusBaseUrl, "_/notifications/setreadstates?" + await MakeQuery(queryDict)), paramDict);
            return jsonTxt;
        }
        public async Task<string> ConnectToGsuc(HttpClient client, Uri plusBaseUrl)
        {
            var url = new Uri(plusBaseUrl, "_/n/gsuc");
            var jsonTxt = await GetStringAsync(client, url);
            return jsonTxt;
        }
        public async Task<string> ConnectToPhotosAlbums(HttpClient client, Uri plusBaseUrl, string plusId, string albumId = null, int offset = 0)
        {
            var query = await MakeQuery(
                new Dictionary<string, string>()
                {
                    { "sdb", "ja" },
                    { "offset", offset.ToString() },
                    { "rt", "j" },
                });
            var jsonTxt = await GetStringAsync(
                client, new Uri(plusBaseUrl, string.Format("_/photos/{0}/albums{1}{2}",
                plusId, albumId != null ? "/" + albumId : "", query != "" ? "?" + query : "")));
            return jsonTxt;
        }
        public async Task<string> ConnectToPhotosAlbumComments(HttpClient client, Uri plusBaseUrl, string plusId, string albumId)
        {
            var jsonTxt = await GetStringAsync(
                client, new Uri(plusBaseUrl, string.Format("_/photos/albumcomments/{0}?albumId={1}", plusId, albumId)));
            return jsonTxt;
        }
        public async Task<string> ConnectToPhotosLightbox(HttpClient client, Uri plusBaseUrl, string plusId, string photoId)
        {
            //_/photos/lightbox/photo/{0}/{1}?soc-app=2&cid=0&soc-platform=1&hl=ja&_reqid=2857574&rt=j
            var jsonTxt = (await client
                .GetStringAsync(new Uri(plusBaseUrl, string.Format("_/photos/lightbox/photo/{0}/{1}?soc-app=2&cid=0&soc-platform=1&hl=ja&_reqid=2857574&rt=j", plusId, photoId))))
                .Substring(25);
            jsonTxt = jsonTxt.Substring(0, jsonTxt.Length - 9);
            return jsonTxt;
        }
        public async Task<string> ConnectToCompleteSearch(HttpClient client, Uri plusBaseUrl, string tok, string searchQuery)
        {
            var authUser = plusBaseUrl.Segments[2].TrimEnd('/');
            plusBaseUrl = new Uri(plusBaseUrl.AbsoluteUri.Substring(0, plusBaseUrl.AbsoluteUri.Length - plusBaseUrl.AbsolutePath.Length) + '/');
            var query = await MakeQuery(new Dictionary<string, string>(){
                { "client", "es-sharebox-search" },
                { "tok", tok },
                { "authuser", authUser },
                { "xhr", "t" },
                { "q", searchQuery },
            });
            var url = new Uri(plusBaseUrl, string.Format("complete/search?{0}", query));
            var jsonTxt = await client.GetStringAsync(url);
            return jsonTxt;
        }
        public IObservable<JToken> ConnectToTalkGadgetBind(HttpClient normalClient, HttpClient streamClient, Uri talkBaseUrl, CookieContainer checkTargetCookies, string pvtVal)
        {
            var observable = Observable.Create<JToken>(subject =>
            {
                var tokenSource = new System.Threading.CancellationTokenSource();
                var task = Task.Run(async () =>
                {
                    try
                    {
                        //通信で使用されるパラメータ。各段階で使用される変数が違うが、
                        //jsonRecieverで一括定義なので冒頭に宣言する必要があった

                        //aidVal: 再接続をする際に取得済み配列が返されないようにするために送る値
                        int aidVal = 0;
                        //各段階で必要なセッションID類
                        string sidVal = null, gsidVal = null, clidVal = null;

                        //受信したjson配列には一定の形式がある。各要素の種類に
                        //合わせた処理をここで一括定義
                        Action<JToken> jsonReciever = (jsonItem) =>
                        {
                            aidVal = (int)jsonItem[0];
                            var data = jsonItem[1];
                            JToken tmp;
                            switch ((string)data[0])
                            {
                                case "c":
                                    switch ((tmp = data[1]).Type)
                                    {
                                        case JTokenType.String:
                                            sidVal = (string)data[1];
                                            break;
                                        case JTokenType.Array:
                                            if ((tmp = tmp[1]).Type == JTokenType.Array && (string)tmp[0] == "ei")
                                                gsidVal = (string)tmp[1];
                                            break;
                                    }
                                    break;
                            }
                            try
                            { subject.OnNext(jsonItem); }
                            catch (Exception e)
                            { throw new OutsideException("通知先で例外が発生しました。", e); }
                        };

                        //clid, gsidを取得する
                        tokenSource.Token.ThrowIfCancellationRequested();
                        var vals = LoadNotifierClient(normalClient, talkBaseUrl, checkTargetCookies, pvtVal).Result["ds:4"][0];
                        clidVal = (string)vals[7];
                        gsidVal = (string)vals[3];

                        //sid値を取得する
                        tokenSource.Token.ThrowIfCancellationRequested();
                        var url = new Uri(talkBaseUrl, string.Format("talkgadget/_/channel/bind?{0}",
                            await MakeQuery(new Dictionary<string, string>()
                                {
                                    //無いと死ぬ
                                    { "clid", clidVal },
                                    //値は何でもおｋ。けど無いと死ぬ
                                    { "VER", "8" },
                                    { "RID", "64437" },
                                    //無くてもおｋ
                                    { "gsessionid", gsidVal },
                                    { "zx", "n9igqs467cbk" },
                                    //不変値。無くてもおｋ。
                                    { "prop", "homepage" },
                                    { "CVER", "1" },
                                    { "t", "1" },
                                    { "ec", "[\"ci:ec\",1,1,0,\"chat_wcs_20140612.120118_RC3\"]" },
                                })));
                        var res = await PostStringAsync(normalClient, url, new Dictionary<string, string>() { { "count", "0" } }, tokenSource.Token);
                        var json = JToken.Parse(ApiAccessorUtility.ConvertIntoValidJson(res.Substring(res.IndexOf("\n") + 1)));
                        foreach (var item in json)
                            jsonReciever(item);

                        //init
                        tokenSource.Token.ThrowIfCancellationRequested();
                        //post内容が意味不明だが、無いとapiが動かないために必要
                        url = new Uri(talkBaseUrl, string.Format("talkgadget/_/channel/bind?{0}",
                            await MakeQuery(new Dictionary<string, string>()
                                {
                                    //無いと死ぬ
                                    { "SID", sidVal },
                                    //値は何でもおｋ。けど無いと死ぬ
                                    { "RID", "64438" },
                                    //無くてもおｋ
                                    { "gsessionid", gsidVal },
                                    { "clid", clidVal },
                                    //不変値。無くてもおｋ。
                                    { "VER", "8" },
                                    { "prop", "homepage" },
                                    { "AID", aidVal.ToString() },
                                    { "t", "1" },
                                    { "zx", "7anb33bhfgp2" },
                                    { "ec", "[\"ci:ec\",1,1,0,\"chat_wcs_20140612.120118_RC3\"]" },
                                })));
                        res = await PostStringAsync(
                            normalClient, url, new Dictionary<string, string>()
                                {
                                    //reqXの数とcountの数を合わせないと死ぬ
                                    { "count", "1" },
                                    { "ofs", "0" },
                                    { "req0_m", "[\"connect-add-client\"]" },
                                    { "req0_c", clidVal },
                                    { "req0__sc", "c" },
                                }, tokenSource.Token);

                        DateTime debug_streamingTime;
                        while (true)
                        {
                            debug_streamingTime = DateTime.UtcNow;
                            tokenSource.Token.ThrowIfCancellationRequested();
                            //user streaming apiに接続する
                            //memo: 接続持続時間は3.5分が目安
                            url = new Uri(talkBaseUrl, string.Format("talkgadget/_/channel/bind?{0}",
                                await MakeQuery(new Dictionary<string, string>()
                                {
                                    //無いと死ぬ
                                    { "RID", "rpc" },
                                    { "SID", sidVal },
                                    { "CI", "0" },
                                    //無くてもおｋ
                                    { "clid", clidVal },//不要必要がちょっと微妙
                                    { "gsessionid", gsidVal },//接続更新でこいつが変化しているのは何故?
                                    { "AID", aidVal.ToString() },//無くても動くが、既読ページ数のようなものなので、正常動作には必要
                                    //不変値。無くてもおｋ
                                    { "VER", "8" },
                                    { "prop", "homepage" },
                                    { "TYPE", "xmlhttp" },
                                    { "zx", "p5guamwzkeiu" },
                                    { "t", "1" },
                                    { "ec", "[\"ci:ec\",1,1,0,\"chat_wcs_20140612.120118_RC3\"]" },
                                })));
                            using (var strm = await GetStreamAsync(streamClient, url, tokenSource.Token))
                            using (var reader = new System.IO.StreamReader(strm, Encoding.UTF8))
                            {
                                var builder = new StringBuilder();
                                var buffer = new char[1024];
                                var buffCnt = 0;
                                var length = -1;
                                while (!reader.EndOfStream)
                                {
                                    tokenSource.Token.ThrowIfCancellationRequested();
                                    if (length < 0)
                                        length = int.Parse(reader.ReadLine());

                                    buffCnt = await reader.ReadAsync(buffer, 0, Math.Min(buffer.Length, length - builder.Length));
                                    builder.Append(buffer, 0, buffCnt);
                                    if (length == builder.Length)
                                    {
                                        var str = builder.ToString();
                                        var replaced = ApiAccessorUtility.ConvertIntoValidJson(str);
                                        var recieveJson = JToken.Parse(replaced);

                                        //後始末
                                        builder.Clear();
                                        length = -1;

                                        foreach (var item in recieveJson)
                                        {
                                            tokenSource.Token.ThrowIfCancellationRequested();
                                            jsonReciever(item);
                                        }
                                    }
                                }
                            }
                            System.Diagnostics.Debug.WriteLine("DEBUG: StreamSessionTime\r\n{0}", DateTime.UtcNow - debug_streamingTime);
                        }
                    }
                    catch (Exception e)
                    {
                        if (tokenSource.Token.IsCancellationRequested)
                            subject.OnCompleted();
                        else
                            subject.OnError(e);
                    }
                }, tokenSource.Token);

                //購読中断時に呼び出してもらうメソッドを返す
                return tokenSource.Cancel;
            });
            return observable;
        }
        public async Task<Tuple<string, string>> ConnectToSearch(HttpClient client, Uri plusBaseUrl, string keyword, SearchTarget target, SearchRange range, string searchTicket, string atVal)
        {
            keyword = keyword.Replace("\\", "\\\\");
            keyword = keyword.Replace("\"", "\\\"");
            var query = searchTicket == null
                ? new Dictionary<string, string>()
                    {
                        { "at", atVal },
                        { "srchrp", string.Format("[[\"{0}\",{1},2,[{2}]]]", keyword, (int)target, (int)range) }
                    }
                : new Dictionary<string, string>()
                    {
                        { "at", atVal },
                        { "srchrp", string.Format("[[\"{0}\",{1},2,[{2}]],null,[\"{3}\"]]", keyword, (int)target, (int)range, searchTicket) }
                    };
            var jsonTxt = ApiAccessorUtility.ConvertIntoValidJson(
                await PostStringAsync(client, new Uri(plusBaseUrl, "_/s/query?rt=j"), query));
            var json = JToken.Parse(jsonTxt);
            searchTicket = (string)json[0][1][1][1][2];
            return Tuple.Create(jsonTxt, searchTicket);
        }
        public async Task<Tuple<string, string>> ConnectToForwardSearch(HttpClient client, Uri plusBaseUrl, string keyword, string searchTicket, string atVal)
        {
            keyword = keyword.Replace("\\", "\\\\");
            keyword = keyword.Replace("\"", "\\\"");
            var query = new Dictionary<string, string>()
                {
                    { "at", atVal },
                    { "srchrp", string.Format("[[\"{0}\",3,2],null,[\"{1}\",false]]", keyword, searchTicket) }
                };
            var jsonTxt = ApiAccessorUtility.ConvertIntoValidJson(
                await PostStringAsync(client, new Uri(plusBaseUrl, "_/s/rt?rt=j"), query));
            var json = JToken.Parse(jsonTxt);
            searchTicket = (string)json[0][1][1][1][2];
            return Tuple.Create(jsonTxt, searchTicket);
        }
        public async Task ConnectToMutateBlockUser(HttpClient client, Uri plusBaseUrl, Tuple<string, string>[] userIdAndNames, AccountBlockType blockType, BlockActionType status, string atVal)
        {
            var blockUrl = new Uri(plusBaseUrl, "_/socialgraph/mutate/block_user/?_reqid=8260308&rt=j");
            var param = string.Format("[[{0}],{1}]",
                string.Join(",", userIdAndNames.Select(tuple => string.Format("[[null,null,\"{0}\"], \"{1}\"]", tuple.Item1, tuple.Item2))),
                status == BlockActionType.Add ? "true" : "false");
            var postVals = new Dictionary<string, string>() { { "m", param }, { "at", atVal } };
            if (blockType == AccountBlockType.Ignore)
                postVals.Add("i", "1");
            await PostStringAsync(client, blockUrl, postVals);
        }
        //htmlパース
        public async Task<Dictionary<string, JToken>> LoadNotifierClient(HttpClient client, Uri talkBaseUrl, CookieContainer checkTargetCookies, string pvtVal)
        {
            var stime = GetUnixTime(DateTime.Now);
            var xpcUrl = string.Format("{{\"cn\":\"rqlvmh\",\"tp\":1,\"ifrid\":\"gtn-roster-iframe-id\",\"pu\":\"{0}\"}}", new Uri(talkBaseUrl, "talkgadget/_/").AbsoluteUri);
            var url = new Uri(talkBaseUrl, string.Format("talkgadget/_/chat?{0}", await MakeQuery(
                new Dictionary<string, string>()
                {
                    //無いと死ぬ
                    { "fid", "gtn-roster-iframe-id" },
                    { "prop", "homepage" },//死にはしないけど、これが無いとclidやらがG+ではなくGTalk用になってしまう
                    //無くてもおｋ
                    { "client", "sm" },//これを入れて通信時にUserAgentを省くとログインに失敗する
                    { "nav", "true" },
                    { "os", "Win32" },
                    { "stime", stime.ToString() },
                    { "xpc", xpcUrl },
                    { "pvt", pvtVal },
                    { "hl", "ja" },
                    { "pal", "1" },
                    { "host", "1" },
                    { "hpc", "true" },
                    { "pos", "l" },
                    { "sl", "false" },
                    { "uiv", "2" },
                    { "uqp", "false" },
                    //不明
                    { "ec", "[\"ci:ec\",true,true,false]" },
                    { "hsm", "true" },
                    { "hrc", "true" },
                    { "mmoleh", "36" },
                    { "two", "https://plus.google.com" },

                    { "rel", "1" },
                    {"zx", "f0xx0w1pefal" },
                })));

            //talkgadgetのcookieが無い場合は認証ページを噛ませる
            //var talkCookie = checkTargetCookies.GetCookies(url);
            //url = talkCookie != null && talkCookie.OfType<Cookie>().Any(dt => dt.Domain.Contains("talkgadget"))
            //    ? url : await WrapTalkGadgetAuthUrl(url);

            var nClientHtm = await GetStringAsync(client, url);
            var match = System.Text.RegularExpressions.Regex.Match(nClientHtm, "AF_initDataCallback\\((?<json>\\{key:[^;]+\\})\\);");
            var resJson = new Dictionary<string, JToken>();
            while (match.Success)
            {
                var jsonTxt = match.Groups["json"].Value;
                match = match.NextMatch();
                if (jsonTxt.Contains("data:function()"))
                    continue;
                var json = JContainer.Parse(ApiAccessorUtility.ConvertIntoValidJson(jsonTxt));
                resJson.Add((string)json["key"], json["data"]);
            }
            return resJson;
        }
        public async Task<string> LoadHomeInitData(HttpClient client, Uri plusBaseUrl)
        { return await GetStringAsync(client, plusBaseUrl); }
        public async Task<string> LoadListAccounts(HttpClient client)
        {
            var accountListUrl = new Uri("https://accounts.google.com/b/0/ListAccounts?listPages=0&origin=https%3A%2F%2Fplus.google.com");
            var regex = new System.Text.RegularExpressions.Regex("window.parent.postMessage\\([^\"]*\"(?<jsonTxt>(?:[^\"])*)\"");

            var accountListPage = await client.GetStringAsync(accountListUrl);
            var jsonTxt = Uri.UnescapeDataString(regex.Match(accountListPage).Groups["jsonTxt"].Value.Replace("\\x", "%"));
            if (jsonTxt == string.Empty)
                throw new ApiErrorException("アカウント一覧の読み込みに失敗しました。", ErrorType.UnknownError, accountListUrl, null, null, null);
            return jsonTxt;
        }
        public async Task<Uri> WrapTalkGadgetAuthUrl(Uri continueUrl)
        {
            const string talkgadgetGAuth = "https://talkgadget.google.com/talkgadget/gauth?{0}";
            const string talkgadgetServiceLogin = "https://accounts.google.com/ServiceLogin?{0}";

            var queryA = await MakeQuery(
                new Dictionary<string, string>()
                {
                    { "redirect", "true" },
                    { "host", continueUrl.AbsoluteUri },
                    { "silent", "true" },
                    { "authuser", "0" }
                });
            var gauthUrl = string.Format(talkgadgetGAuth, queryA);

            var queryB = await MakeQuery(
                new Dictionary<string, string>()
                {
                    { "service", "talk" },
                    { "passive", "true" },
                    { "skipvpage", "true" },
                    { "continue", gauthUrl },
                    { "go", "true" }
                });
            var serviceLoginUrl = string.Format(talkgadgetServiceLogin, queryB);
            return new Uri(serviceLoginUrl);
        }

        //支援関数
        public static ulong GetUnixTime(DateTime date)
        {
            var stime = (ulong)((date - DateUnixEpoch).TotalMilliseconds);
            return stime;
        }
        public static DateTime GetDateTime(ulong unixTime)
        {
            var date = DateUnixEpoch.AddMilliseconds(unixTime);
            return TimeZoneInfo.ConvertTime(date, System.TimeZoneInfo.Utc);
        }
        static Task<string> MakeQuery(Dictionary<string, string> data)
        {
            using (var content = new FormUrlEncodedContent(data))
                return content.ReadAsStringAsync();
        }
        //不本意ながらモック化を意図してpublic virtualで定義している。実際はprivate staticで十分
        public virtual async Task<string> PostStringAsync(HttpClient client, Uri requestUrl, Dictionary<string, string> content, System.Threading.CancellationToken? token = null)
        {
            try
            {
                var htContent = new FormUrlEncodedContent(content);
                var res = await client.PostAsync(requestUrl, htContent, token ?? System.Threading.CancellationToken.None);
                if (res.IsSuccessStatusCode)
                    return await res.Content.ReadAsStringAsync();
                else
                {
                    switch (res.StatusCode)
                    {
                        case HttpStatusCode.Forbidden:
                            throw new ApiErrorException("G+API参照に失敗。ログインセッションが失効しています。" + res.StatusCode, ErrorType.SessionError, requestUrl, null, res, null);
                        case HttpStatusCode.NotFound:
                            throw new ApiErrorException("G+API参照に失敗。参照された内容は存在しません。" + res.StatusCode, ErrorType.ParameterError, requestUrl, null, res, null);
                        default:
                            throw new ApiErrorException("G+API参照に失敗。G+側に何らかの障害が発生しています。" + res.StatusCode, ErrorType.UnknownError, requestUrl, null, res, null);
                    }
                }
            }
            catch (HttpRequestException e)
            {
                if (e.InnerException is WebException)
                    throw new ApiErrorException("G+API参照に失敗。通信中に接続が切断されました。", ErrorType.NetworkError, requestUrl, null, null, e);
                else
                    throw new ApiErrorException("G+API参照に失敗。想定していないエラーが発生しました。", ErrorType.NetworkError, requestUrl, null, null, e);
            }
        }
        public virtual async Task<string> GetStringAsync(HttpClient client, Uri requestUrl, System.Threading.CancellationToken? token = null)
        {
            try
            {
                var res = await client.GetAsync(requestUrl, token ?? System.Threading.CancellationToken.None);
                if (res.IsSuccessStatusCode)
                    return await res.Content.ReadAsStringAsync();
                else
                {
                    switch (res.StatusCode)
                    {
                        case HttpStatusCode.Forbidden:
                            throw new ApiErrorException("G+API参照に失敗。ログインセッションが失効しています。", ErrorType.SessionError, requestUrl, null, res, null);
                        case HttpStatusCode.NotFound:
                            throw new ApiErrorException("G+API参照に失敗。参照された内容は存在しません。", ErrorType.ParameterError, requestUrl, null, res, null);
                        default:
                            throw new ApiErrorException("G+API参照に失敗。G+側に何らかの障害が発生しています。", ErrorType.UnknownError, requestUrl, null, res, null);
                    }
                }
            }
            catch (HttpRequestException e)
            {
                if (e.InnerException is WebException)
                    throw new ApiErrorException("G+API参照に失敗。通信中に接続が切断されました。", ErrorType.NetworkError, requestUrl, null, null, e);
                else
                    throw new ApiErrorException("G+API参照に失敗。想定していないエラーが発生しました。", ErrorType.NetworkError, requestUrl, null, null, e);
            }
        }
        public virtual async Task<System.IO.Stream> GetStreamAsync(HttpClient client, Uri requestUrl, System.Threading.CancellationToken? token = null)
        {
            try
            {
                var res = await client.GetAsync(
                    requestUrl, HttpCompletionOption.ResponseHeadersRead,
                    token ?? System.Threading.CancellationToken.None);
                if (res.IsSuccessStatusCode)
                    return await res.Content.ReadAsStreamAsync();
                else
                {
                    switch (res.StatusCode)
                    {
                        case HttpStatusCode.Forbidden:
                            throw new ApiErrorException("G+API参照に失敗。ログインセッションが失効しています。", ErrorType.SessionError, requestUrl, null, res, null);
                        case HttpStatusCode.NotFound:
                            throw new ApiErrorException("G+API参照に失敗。参照された内容は存在しません。", ErrorType.ParameterError, requestUrl, null, res, null);
                        default:
                            throw new ApiErrorException("G+API参照に失敗。G+側に何らかの障害が発生しています。", ErrorType.UnknownError, requestUrl, null, res, null);
                    }
                }
            }
            catch (HttpRequestException e)
            {
                if (e.InnerException is WebException)
                    throw new ApiErrorException("G+API参照に失敗。通信中に接続が切断されました。", ErrorType.NetworkError, requestUrl, null, null, e);
                else
                    throw new ApiErrorException("G+API参照に失敗。想定していないエラーが発生しました。", ErrorType.NetworkError, requestUrl, null, null, e);
            }
        }
    }

    class OutsideException : Exception
    { public OutsideException(string message, Exception innerException) : base(message, innerException) { } }
}
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

    public static class ApiWrapper
    {
        public static readonly DateTime DateUnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static readonly string SilhouetteImageUrl = "https://ssl.gstatic.com/s2/profiles/images/silhouette$SIZE_NUM.png";
        static readonly string SilhouetteImagePattern = "https://ssl.gstatic.com/s2/profiles/images/silhouette";

        [Obsolete("このメソッドはGoogleサーバー側に不正アクセスと誤認識される事があるため、使用すべきではありません。")]
        public static async Task<bool> ConnectToServiceLoginAuth(HttpClient client, Uri plusBaseUrl, System.Net.CookieContainer responseCheckTarget, string email, string password)
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
        public static async Task<JToken> ConnectToInitialData(HttpClient client, Uri plusBaseUrl, int key, string atVal)
        {
            var url = new Uri(plusBaseUrl, string.Format("_/initialdata?key={0}&rt=j", key));
            var jsonTxt = (await PostStringAsync(client, url, new FormUrlEncodedContent(new Dictionary<string, string>() { { "at", atVal }, })))
                .Substring(6);
            var json = JToken.Parse(ConvertIntoValidJson(jsonTxt));
            return json;
        }
        public static async Task<JToken> ConnectToGetIdentities(HttpClient client, Uri plusBaseUrl)
        {
            //api error: innerEx == null
            //ses error: innerEx is WebException
            var url = new Uri(plusBaseUrl, "_/pages/getidentities/?hl=ja&rt=j");
            var jsonTxt = (await GetStringAsync(client, url)).Substring(6);
            var json = JToken.Parse(ConvertIntoValidJson(jsonTxt));
            return json;
        }
        public static async Task<JToken> ConnectToProfileGet(HttpClient client, Uri plusBaseUrl, string plusId)
        {
            var url = new Uri(plusBaseUrl, string.Format("_/profiles/get/{0}/posts?hl=ja&rt=j", plusId));
            var jsonTxt = (await GetStringAsync(client, url)).Substring(6);
            var json = JToken.Parse(ConvertIntoValidJson(jsonTxt));
            return json;
        }
        public static async Task<JToken> ConnectToLookupPeople(HttpClient client, Uri plusBaseUrl, string plusId, string atVal)
        {
            var jsonTxt = await PostStringAsync(
                client,
                new Uri(plusBaseUrl, "_/socialgraph/lookup/people/?if=true&rt=j"),
                new FormUrlEncodedContent(new Dictionary<string, string>()
                    {
                        { "m", string.Format("[[[null,null,\"{0}\"]]]", plusId) },
                        { "at", atVal },
                    }));
            var json = JToken.Parse(ConvertIntoValidJson(jsonTxt.Substring(6)));
            return json;
        }
        public static async Task<JToken> ConnectToLookupCircles(HttpClient client, Uri plusBaseUrl, string atValue)
        {
            var circlesUrl = new Uri(plusBaseUrl, "_/socialgraph/lookup/circles/?ct=2&m=true&rt=j");
            var jsonTxt = await PostStringAsync(client, circlesUrl, new FormUrlEncodedContent(new Dictionary<string, string>() { { "at", atValue } }));
            return JToken.Parse(ConvertIntoValidJson(jsonTxt.Substring(6)));
        }
        public static async Task<JToken> ConnectToLookupFollowers(HttpClient client, Uri plusBaseUrl, string atValue)
        {
            var url = new Uri(plusBaseUrl, "_/socialgraph/lookup/followers/?m=2500&rt=j");
            var jsonTxt = await PostStringAsync(
                client, url, new FormUrlEncodedContent(new Dictionary<string, string>() { { "at", atValue } }));
            return JToken.Parse(ConvertIntoValidJson(jsonTxt.Substring(6)));
        }
        public static async Task<JToken> ConnectToLookupIgnore(HttpClient client, Uri plusBaseUrl, string atValue)
        {
            var ignoreUrl = new Uri(plusBaseUrl, "_/socialgraph/lookup/ignored/?m=5000&rt=j");
            var jsonTxt = await PostStringAsync(
                client, ignoreUrl, new FormUrlEncodedContent(new Dictionary<string, string>() { { "at", atValue } }));
            return JToken.Parse(ConvertIntoValidJson(jsonTxt.Substring(6)));
        }
        public static async Task<JToken> ConnectToLookupVisible(HttpClient client, Uri plusBaseUrl, string plusId, string atValue)
        {
            var jsonTxt = await PostStringAsync(
                client,
                new Uri(plusBaseUrl, "_/socialgraph/lookup/visible/"),
                new FormUrlEncodedContent(new Dictionary<string, string>()
                        {
                            { "o", string.Format("[null,null,\"{0}\"]", plusId) },
                            { "rt", "j" },
                            { "at", atValue },
                        }));
            return JToken.Parse(ConvertIntoValidJson(jsonTxt.Substring(6)));
        }
        public static async Task<JToken> ConnectToLookupIncoming(HttpClient client, Uri plusBaseUrl, string plusId, int count, string atValue)
        {
            var jsonTxt = await PostStringAsync(
                client,
                new Uri(plusBaseUrl, "_/socialgraph/lookup/incoming/"),
                new FormUrlEncodedContent(new Dictionary<string, string>()
                        {
                            { "o", string.Format("[null,null,\"{0}\"]", plusId) },
                            { "s", "true" },
                            { "n", count.ToString() },
                            { "rt", "j" },
                            { "at", atValue },
                        }));
            return JToken.Parse(ConvertIntoValidJson(jsonTxt.Substring(6)));
        }
        public static async Task<JToken> ConnectToGetActivities(HttpClient client, Uri plusBaseUrl, string atVal, int length = 20, string circleId = null, string plusId = null, string ct = null)
        {
            //query作成
            var query = new FormUrlEncodedContent(
                new Dictionary<string, string>()
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
                    });

            //download
            var resStr = await PostStringAsync(client, new Uri(plusBaseUrl, "_/stream/getactivities/?rt=j"), query);
            return JToken.Parse(ConvertIntoValidJson(resStr.Substring(6)));
        }
        public static async Task<JToken> ConnectToGetActivity(HttpClient client, Uri plusBaseUrl, string id)
        {
            var resStr = await GetStringAsync(
                client, new Uri(plusBaseUrl, string.Format("_/stream/getactivity/?updateId={0}", id)));
            return JToken.Parse(ConvertIntoValidJson(resStr.Substring(6)));
        }
        public static async Task<JToken> ConnectToPost(HttpClient client, Uri plusBaseUrl, DateTime postDate, int postCount, string plusId, Dictionary<string, string> targetCircles, Dictionary<string, string> targetUsers, ContentType? attachedContentType, string content, bool isDisabledComment, bool isDisabledReshare, string atVal)
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
            var parameter = new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "spar", (string)sparJson },
                    { "at", atVal }
                });
            var url = new Uri(plusBaseUrl, "_/sharebox/post/?spam=20&rt=j");
            var jsonTxt = (await PostStringAsync(client, url, parameter)).Substring(6);
            var json = JToken.Parse(ConvertIntoValidJson(jsonTxt));
            return json;
        }
        public static async Task<JToken> ConnectToComment(HttpClient client, Uri plusBaseUrl, string activityId, string content, DateTime postDate, string atVal)
        {
            var url = new Uri(plusBaseUrl, "_/stream/comment/?rt=j");
            var prmsStr = new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "itemId", activityId },
                    { "clientId", string.Format("os:{0}:{1}", activityId, postDate) },
                    { "text", content },
                    { "timestamp_msec", GetUnixTime(postDate).ToString() },
                    { "at", atVal },
                });
            var jsonTxt = (await PostStringAsync(client, url, prmsStr)).Substring(6);
            var json = JToken.Parse(ConvertIntoValidJson(jsonTxt));
            return json;
        }
        public static async Task<JToken> ConnectToEditComment(HttpClient client, Uri plusBaseUrl, string activityId, string commentId, string content, string atVal)
        {
            var url = new Uri(plusBaseUrl, "_/stream/editcomment/?rt=j");
            var query = new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "itemId", activityId },
                    { "commentId", commentId },
                    { "text", content },
                    { "at", atVal },
                });
            var jsonTxt = (await PostStringAsync(client, url, query)).Substring(6);
            var json = JToken.Parse(ConvertIntoValidJson(jsonTxt));
            return json;
        }
        public static async Task<JToken> ConnectToDeleteComment(HttpClient client, Uri plusBaseUrl, string commentId, string atVal)
        {
            var url = new Uri(plusBaseUrl, "_/stream/deletecomment/?rt=j");
            var query = new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "commentId", commentId },
                    { "at", atVal },
                });
            var jsonTxt = (await PostStringAsync(client, url, query)).Substring(6);
            var json = JToken.Parse(ConvertIntoValidJson(jsonTxt));
            return json;
        }
        public static async Task<JToken> ConnectToPlusOne(HttpClient client, Uri plusBaseUrl, string targetId, bool isPlusOned, string atVal)
        {
            var url = new Uri(plusBaseUrl, "_/plusone?rt=j");
            var query = new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "itemId", targetId },
                    { "set", isPlusOned ? "true" : "false" },
                    { "at", atVal },
                });
            var jsonTxt = (await PostStringAsync(client, url, query)).Substring(6);
            return JToken.Parse(ConvertIntoValidJson(jsonTxt));
        }
        public static async Task<JToken> ConnectToCommonGetPeople(HttpClient client, Uri plusBaseUrl, string plusoneId, int length, string atVal)
        {
            var url = new Uri(plusBaseUrl, "_/common/getpeople/?rt=j");
            var query = new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "plusoneId", plusoneId },
                    { "num", length.ToString() },
                    { "hl", "ja" },
                    { "at", atVal },
                });
            var jsonTxt = (await PostStringAsync(client, url, query)).Substring(6);
            return JToken.Parse(ConvertIntoValidJson(jsonTxt));
        }
        public static async Task<JToken> ConnectToNotificationsData(HttpClient client, Uri plusBaseUrl, string atVal, NotificationsFilter type = NotificationsFilter.All, int maxResults = 15, string continueToken = null)
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
            var paramArray = new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "f.req", string.Format("[{0},[],6,null,[],null,null,[],null,{1},{2},null,null,null,null,null,[18],null,null,2]", type == NotificationsFilter.All ? "null" : ((int)type).ToString(), continueToken != null ? "\"" + continueToken + "\"" : "null", maxResults) },
                    { "at", atVal }
                });
            var query = await MakeQuery(queryArray);
            var jsonTxt = (await PostStringAsync(client, new Uri(plusBaseUrl, "_/notifications/getnotificationsdata?" + query), paramArray));
            var json = JToken.Parse(ApiWrapper.ConvertIntoValidJson(jsonTxt.Substring(6)));
            return json;
        }
        public static async Task<JToken> ConnectToNotificationsFetch(HttpClient client, Uri plusBaseUrl, string atVal, NotificationsFilter type = NotificationsFilter.All, int maxResults = 15, string continueToken = null)
        {
            //
            var queryArray = new Dictionary<string, string>()
                {
                    { "soc-app", "1" },
                    { "cid", "0" },
                    { "soc-platform", "1" },
                    { "hl", "ja" },
                    { "avw", "str:1" },
                    { "rt","j" },
                };
            var paramArray = new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    //[[\"OGB\",[7]],[null,null,10,[],[1],null,\"GPLUS_APP\",[3]],[3]]
                    { "f.req", string.Format("[{0},[],5,null,[],null,true,[],null,null,{1},null,2]", type == NotificationsFilter.All ? "null" : ((int)type).ToString(), maxResults) },
                    { "at", atVal }
                });
            if (type != NotificationsFilter.All)
                queryArray.Add("filter", ((int)type).ToString());
            if (continueToken != null)
                queryArray.Add("continuationToken", continueToken);
            var query = await MakeQuery(queryArray);
            var jsonTxt = (await PostStringAsync(client, new Uri(plusBaseUrl, "_/notifications/fetch?" + query), paramArray));
            var json = JToken.Parse(ApiWrapper.ConvertIntoValidJson(jsonTxt.Substring(6)));
            return json;
        }
        public static async Task<JToken> ConnectToNotificationsUpdateLastReadTime(HttpClient client, Uri plusBaseUrl, DateTime lastReadTime, string atValue)
        {
            var query = new FormUrlEncodedContent(new Dictionary<string, string>()
            {
                { "time", (GetUnixTime(lastReadTime) * 1000).ToString() },
                { "hl", "ja" },
                { "at", atValue }
            });
            var jsonTxt = (await PostStringAsync(
                client,
                new Uri(plusBaseUrl, "_/notifications/updatelastreadtime?rt=j"), query)).Substring(6);
            var json = JToken.Parse(ConvertIntoValidJson(jsonTxt));
            return json;
        }
        public static async Task<JToken> ConnectToPhotosAlbums(HttpClient client, Uri plusBaseUrl, string plusId, string albumId = null, int offset = 0)
        {
            var query = await MakeQuery(
                new Dictionary<string, string>()
                {
                    { "sdb", "ja" },
                    { "offset", offset.ToString() },
                    { "rt", "j" },
                });
            var jsonTxt = (await GetStringAsync(
                client, new Uri(plusBaseUrl, string.Format("_/photos/{0}/albums{1}{2}",
                plusId, albumId != null ? "/" + albumId : "", query != "" ? "?" + query : "")))).Substring(6);
            var json = JToken.Parse(ConvertIntoValidJson(jsonTxt));
            return json;
        }
        public static async Task<JToken> ConnectToPhotosAlbumComments(HttpClient client, Uri plusBaseUrl, string plusId, string albumId)
        {
            var jsonTxt =
                (await GetStringAsync(
                    client, new Uri(plusBaseUrl, string.Format("_/photos/albumcomments/{0}?albumId={1}", plusId, albumId))))
                .Substring(6);
            var json = JToken.Parse(ConvertIntoValidJson(jsonTxt));
            return json;
        }
        public static async Task<JToken> ConnectToPhotosLightbox(HttpClient client, Uri plusBaseUrl, string plusId, string photoId)
        {
            //_/photos/lightbox/photo/{0}/{1}?soc-app=2&cid=0&soc-platform=1&hl=ja&_reqid=2857574&rt=j
            var jsonTxt = (await client
                .GetStringAsync(new Uri(plusBaseUrl, string.Format("_/photos/lightbox/photo/{0}/{1}?soc-app=2&cid=0&soc-platform=1&hl=ja&_reqid=2857574&rt=j", plusId, photoId))))
                .Substring(25);
            jsonTxt = jsonTxt.Substring(0, jsonTxt.Length - 9);
            var json = JToken.Parse(ConvertIntoValidJson(jsonTxt));
            return json;
        }
        public static IObservable<JToken> ConnectToTalkGadgetBind(HttpClient normalClient, HttpClient streamClient, Uri talkBaseUrl, CookieContainer checkTargetCookies, string pvtVal)
        {
            var observable = Observable.Create<JToken>(subject =>
            {
                var tokenSource = new System.Threading.CancellationTokenSource();
                var task = Task.Factory.StartNew(async () =>
                {
                    try
                    {
                        tokenSource.Token.ThrowIfCancellationRequested();
                        var talkCookies = checkTargetCookies.GetCookies(new Uri("https://talkgadget.google.com"));
                        var vals = LoadNotifierClient(normalClient, talkBaseUrl, checkTargetCookies, pvtVal).Result;
                        var clidVal = (string)vals["clid"];
                        var gsidVal = (string)vals["gsessionid"];

                        //sid値を取得する
                        tokenSource.Token.ThrowIfCancellationRequested();
                        var url = new Uri(talkBaseUrl, string.Format("talkgadget/_/channel/bind?{0}",
                            await MakeQuery(new Dictionary<string, string>()
                                {
                                    //無いと死ぬ
                                    { "clid", clidVal },
                                    //値は何でもおｋ。けど無いと死ぬ
                                    { "VER", "8" },
                                    { "RID", "27348" },
                                    //無くてもおｋ
                                    { "gsessionid", gsidVal },
                                    //不変値。無くてもおｋ。
                                    { "prop", "homepage" },
                                    { "CVER", "1" },
                                    { "t", "1" },
                                    { "ec", "[1,1,0,\"chat_wcs_20131102.193042_RC3\"]" },
                                })));
                        var res = await PostStringAsync(normalClient, url, new Dictionary<string, string>() { { "count", "0" } }, tokenSource.Token);
                        var json = JToken.Parse(ConvertIntoValidJson(res.Substring(res.IndexOf("\n") + 1)));

                        //ストリーミングapiの再接続をする際に取得済み配列が返されないようにするために
                        //クエリとして送るAID値です。内容は今まで取得したjson配列の最終インデックス値です。
                        //また、取得したjson配列の最後の要素に含まれるidでもあります。
                        //ちなみに上の通信で帰ってくるjsonの最終インデックスは2。
                        var aidVal = (int)json.Last[0];
                        var sidVal = (string)json[0][1][1];
                        gsidVal = (string)json[2][1][1][1][1];

                        tokenSource.Token.ThrowIfCancellationRequested();
                        //post内容が意味不明だが、無いとapiが動かないために必要
                        url = new Uri(talkBaseUrl, string.Format("talkgadget/_/channel/bind?{0}",
                            await MakeQuery(new Dictionary<string, string>()
                                {
                                    //無いと死ぬ
                                    { "SID", sidVal },
                                    //値は何でもおｋ。けど無いと死ぬ
                                    { "RID", "27349" },
                                    //無くてもおｋ
                                    { "gsessionid", gsidVal },
                                    { "clid", clidVal },
                                    //不変値。無くてもおｋ。
                                    { "VER", "8" },
                                    { "prop", "homepage" },
                                    { "AID", aidVal.ToString() },
                                    { "t", "1" },
                                    { "zx", "7anb33bhfgp2" },
                                    { "ec", "[1,1,0,\"chat_wcs_20131102.193042_RC3\"]" },
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

                        while (true)
                        {
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
                                    { "zx", "9ngcqx5la3k" },
                                    { "t", "1" },
                                    { "ec", "[1,1,0,\"chat_wcs_20131102.193042_RC3\"]" },
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

                                    buffCnt = reader.Read(buffer, 0, Math.Min(buffer.Length, length - builder.Length));
                                    builder.Append(buffer, 0, buffCnt);
                                    if (length == builder.Length)
                                    {
                                        var str = builder.ToString();
                                        var replaced = ConvertIntoValidJson(str);
                                        var recieveJson = JToken.Parse(replaced);

                                        //後始末
                                        builder.Clear();
                                        length = -1;

                                        foreach (var item in recieveJson)
                                        {
                                            tokenSource.Token.ThrowIfCancellationRequested();
                                            if ((string)item[1][0] == "c")
                                                switch ((string)item[1][1][1][0])
                                                {
                                                    case "ei":
                                                        gsidVal = (string)item[1][1][1][1];
                                                        break;
                                                }

                                            try
                                            { subject.OnNext(item); }
                                            catch (Exception e)
                                            { throw new OutsideException("通知先で例外が発生しました。", e); }
                                            aidVal++;
                                        }
                                    }
                                }
                            }
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
        public static async Task<Tuple<JToken, string>> ConnectToSearch(HttpClient client, Uri plusBaseUrl, string keyword, SearchTarget target, SearchRange range, string searchTicket, string atVal)
        {
            keyword = keyword.Replace("\\", "\\\\");
            keyword = keyword.Replace("\"", "\\\"");
            var query = searchTicket == null
                ? new FormUrlEncodedContent(new Dictionary<string, string>()
                    {
                        { "at", atVal },
                        { "srchrp", string.Format("[[\"{0}\",{1},2,[{2}]]]", keyword, (int)target, (int)range) }
                    })
                : new FormUrlEncodedContent(new Dictionary<string, string>()
                    {
                        { "at", atVal },
                        { "srchrp", string.Format("[[\"{0}\",{1},2,[{2}]],null,[\"{3}\"]]", keyword, (int)target, (int)range, searchTicket) }
                    });
            var jsonTxt = (await PostStringAsync(client, new Uri(plusBaseUrl, "_/s/query?rt=j"), query)).Substring(6);
            var json = JToken.Parse(ConvertIntoValidJson(jsonTxt));
            searchTicket = (string)json[0][1][1][1][2];
            return Tuple.Create(json, searchTicket);
        }
        public static async Task<Tuple<JToken, string>> ConnectToForwardSearch(HttpClient client, Uri plusBaseUrl, string keyword, string searchTicket, string atVal)
        {
            keyword = keyword.Replace("\\", "\\\\");
            keyword = keyword.Replace("\"", "\\\"");
            var query = new FormUrlEncodedContent(new Dictionary<string, string>()
                {
                    { "at", atVal },
                    { "srchrp", string.Format("[[\"{0}\",3,2],null,[\"{1}\",false]]", keyword, searchTicket) }
                });
            var jsonTxt = (await PostStringAsync(client, new Uri(plusBaseUrl, "_/s/rt?rt=j"), query)).Substring(6);
            var json = JToken.Parse(ConvertIntoValidJson(jsonTxt));
            searchTicket = (string)json[0][1][1][1][2];
            return Tuple.Create(json, searchTicket);
        }
        public static async Task ConnectToMutateBlockUser(HttpClient client, Uri plusBaseUrl, Tuple<string, string>[] userIdAndNames, AccountBlockType blockType, BlockActionType status, string atVal)
        {
            var blockUrl = new Uri(plusBaseUrl, "_/socialgraph/mutate/block_user/?_reqid=8260308&rt=j");
            var param = string.Format("[[{0}],{1}]",
                string.Join(",", userIdAndNames.Select(tuple => string.Format("[[null,null,\"{0}\"], \"{1}\"]", tuple.Item1, tuple.Item2))),
                status == BlockActionType.Add ? "true" : "false");
            var postVals = new Dictionary<string, string>() { { "m", param }, { "at", atVal } };
            if (blockType == AccountBlockType.Ignore)
                postVals.Add("i", "1");
            await PostStringAsync(client, blockUrl, new FormUrlEncodedContent(postVals));
        }
        //htmlパース
        public static async Task<Dictionary<string, string>> LoadNotifierClient(HttpClient client, Uri talkBaseUrl, CookieContainer checkTargetCookies, string pvtVal)
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
                })));

            //認証して読み込む
            var nClientHtm = await GetStringAsync(client, url);
            var bgnIdx = nClientHtm.IndexOf("\"https://talkgadget.google.com/");
            var endIdx = nClientHtm.IndexOf("gtn-roster-iframe-id\"", bgnIdx + 1);
            var prms = nClientHtm.Substring(bgnIdx, endIdx - bgnIdx);
            var match = System.Text.RegularExpressions.Regex.Match(prms, "\"([^\"]+)\"");

            string clid = null;
            string gsessionid = null;
            for (var i = 0; match.Success; i++)
            {
                switch (i)
                {
                    case 2:
                        clid = match.Groups[1].Value;
                        break;
                    case 4:
                        gsessionid = match.Groups[1].Value;
                        break;
                }
                if (i < 4)
                    match = match.NextMatch();
                else
                    break;
            }

            if (string.IsNullOrEmpty(clid) || string.IsNullOrEmpty(gsessionid))
                throw new ApiErrorException("clid, gsessionidの読み込みに失敗しました。", ErrorType.UnknownError, url, null, null);
            else
                return new Dictionary<string, string>()
                {
                    { "clid", clid },
                    { "gsessionid", gsessionid}
                };
        }
        public static async Task<Dictionary<int, JToken>> LoadHomeInitData(HttpClient client, Uri plusBaseUrl)
        {
            var plusPg = await GetStringAsync(client, plusBaseUrl);
            var matches = System.Text.RegularExpressions.Regex.Matches(plusPg, "\\((?<json>\\{\\s*key\\s*:(?:\"(?:\\\\\"|[^\"])*\"|[^;])*\\})\\);");
            var initDtQ = new Dictionary<int, JToken>();
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var json = JToken.Parse(ConvertIntoValidJson(match.Groups["json"].Value));
                initDtQ.Add(int.Parse((string)json["key"]), json["data"]);
            }
            return initDtQ;
        }

        //支援関数
        public static string ComplementUrl(string urlText, string defaultUrl)
        {
            if (string.IsNullOrEmpty(urlText))
                return defaultUrl;
            else
                return urlText.Substring(0, 6) == "https:" ? urlText : "https:" + urlText;
        }
        public static string ConvertIntoValidJson(string jsonText)
        {
            var builder = new StringBuilder((int)(jsonText.Length * 1.3));
            var ndTypStk = new Stack<NodeType>();
            ndTypStk.Push(NodeType.None);
            var sngl = false;
            for (var i = 0; i < jsonText.Length; i++)
            {
                var chr = jsonText[i];

                switch (ndTypStk.Peek())
                {
                    case NodeType.Array:
                        switch (chr)
                        {
                            case '"':
                            case '\'':
                                ndTypStk.Push(NodeType.ValStr);
                                builder.Append('"');
                                sngl = chr == '\'';
                                break;
                            case ',':
                                switch (jsonText[i - 1])
                                {
                                    case '[':
                                    case ',':
                                        builder.Append("null");
                                        break;
                                }
                                builder.Append(chr);
                                break;
                            case '{':
                                ndTypStk.Push(NodeType.DictKey);
                                builder.Append(chr);
                                break;
                            case '[':
                                ndTypStk.Push(NodeType.Array);
                                builder.Append(chr);
                                break;
                            case ']':
                                switch (jsonText[i - 1])
                                {
                                    //case '[':
                                    case ',':
                                        builder.Append("null");
                                        break;
                                }
                                ndTypStk.Pop();
                                builder.Append(chr);
                                break;
                            case ' ':
                            case '\r':
                            case '\n':
                                builder.Append(chr);
                                break;
                            case '0':
                            case '1':
                            case '2':
                            case '3':
                            case '4':
                            case '5':
                            case '6':
                            case '7':
                            case '8':
                            case '9':
                            case 'n'://null
                            case '-'://負の数
                            case 'f'://false
                            case 't'://true
                                ndTypStk.Push(NodeType.ValNum);
                                i--;
                                break;
                            default:
                                throw new ArgumentException("引数jsonには書式異常があります。");
                        }
                        break;
                    case NodeType.DictKey:
                        switch (chr)
                        {
                            case '"':
                            case '\'':
                                ndTypStk.Push(NodeType.KeyStr);
                                builder.Append('"');
                                sngl = chr == '\'';
                                break;
                            case ' ':
                                builder.Append(chr);
                                break;
                            case ':':
                                builder.Append(chr);
                                ndTypStk.Pop();
                                ndTypStk.Push(NodeType.DictVal);
                                break;
                            //case '0':
                            //case '1':
                            //case '2':
                            //case '3':
                            //case '4':
                            //case '5':
                            //case '6':
                            //case '7':
                            //case '8':
                            //case '9':
                            //    ndTypStk.Push(NodeType.KeyNum);
                            //    builder.Append("\"idx");
                            //    i--;
                            //    sngl = false;
                            //    break;
                            default:
                                ndTypStk.Push(NodeType.KeyNum);
                                builder.Append("\"");
                                i--;
                                sngl = false;
                                break;
                        }
                        break;
                    case NodeType.DictVal:
                        switch (chr)
                        {
                            case '"':
                            case '\'':
                                ndTypStk.Push(NodeType.ValStr);
                                builder.Append('"');
                                sngl = chr == '\'';
                                break;
                            case ' ':
                            case '\r':
                            case '\n':
                                builder.Append(chr);
                                break;
                            case ',':
                                builder.Append(chr);
                                ndTypStk.Pop();
                                ndTypStk.Push(NodeType.DictKey);
                                break;
                            case '0':
                            case '1':
                            case '2':
                            case '3':
                            case '4':
                            case '5':
                            case '6':
                            case '7':
                            case '8':
                            case '9':
                            case 'n'://null
                            case '-'://負の数
                            case 'f'://false
                            case 't'://true
                                ndTypStk.Push(NodeType.ValNum);
                                i--;
                                break;
                            case '[':
                                ndTypStk.Push(NodeType.Array);
                                builder.Append(chr);
                                break;
                            case '{':
                                ndTypStk.Push(NodeType.DictKey);
                                builder.Append(chr);
                                break;
                            case '}':
                                ndTypStk.Pop();
                                builder.Append(chr);
                                break;
                            default:
                                throw new ArgumentException("引数jsonには書式異常があります。");
                        }
                        break;
                    case NodeType.KeyStr:
                    case NodeType.ValStr:
                        var j = i;
                        while (true)
                        {
                            j = sngl ? jsonText.IndexOf('\'', j) : jsonText.IndexOf('"', j);
                            var l = 0;
                            for (; jsonText[j - l - 1] == '\\'; l++) ;
                            if (l % 2 == 0)
                            {
                                builder.Append(string.Concat(jsonText.Substring(i, j - i), '"'));
                                ndTypStk.Pop();
                                i = j;
                                break;
                            }
                            j++;
                        }
                        break;
                    case NodeType.KeyNum:
                    case NodeType.ValNum:
                        char[] ptrn = ndTypStk.Peek() == NodeType.KeyNum
                            ? new char[] { ':' }
                            : new char[] { ',', ']', '}' };

                        var k = jsonText.IndexOfAny(ptrn, i);
                        builder.Append(jsonText.Substring(i, k - i));
                        i = k - 1;
                        if (ndTypStk.Pop() == NodeType.KeyNum)
                            builder.Append('"');
                        break;
                    case NodeType.None:
                        switch (chr)
                        {
                            case '[':
                                ndTypStk.Push(NodeType.Array);
                                builder.Append(chr);
                                break;
                            case '{':
                                ndTypStk.Push(NodeType.DictKey);
                                builder.Append(chr);
                                break;
                            case '\r':
                            case '\n':
                                builder.Append(chr);
                                break;
                            default:
                                throw new ArgumentException("引数jsonには書式異常があります。");
                        }
                        break;
                }
            }
            return builder.ToString();
        }
        public static string DecodeHtmlText(string encodedText)
        {
            if (string.IsNullOrEmpty(encodedText))
                return encodedText;

            var ary = encodedText.Split('&');
            var builder = new StringBuilder(encodedText.Length);

            //初めの要素の先頭には&が含まれる事はない。&が含まれる事によって分割
            //し得る文字列は0番地よりも後になる
            builder.Append(ary[0]);
            for (var i = 1; i < ary.Length; i++)
            {
                var item = ary[i];
                var semicron = item.IndexOf(';');
                var charCodeTxt = item.Substring(0, semicron);
                int charCode;
                if (charCodeTxt[0] == '#')
                {
                    charCode = int.Parse(charCodeTxt.Substring(1));
                    builder.Append((char)charCode);
                }
                else if (entityPairs.TryGetValue(charCodeTxt, out charCode))
                {
                    builder.Append(((char)charCode).ToString());
                }
                else
                    throw new Exception("不明の実体参照があります。");
                builder.Append(item.Substring(semicron + 1));
            }
            return builder.ToString();
        }
        public static string ConvertReplasableUrl(string urlText)
        {
            urlText = ComplementUrl(urlText, SilhouetteImageUrl);
            if (urlText.Substring(0, SilhouetteImagePattern.Length) == SilhouetteImagePattern)
                urlText = SilhouetteImageUrl;
            else
            {
                int fileSeg, argSeg;
                if ((fileSeg = urlText.LastIndexOf('/')) < 0 || (argSeg = urlText.LastIndexOf('/', fileSeg - 1)) < 0)
                    throw new ArgumentException("引数urlTextのサイズセグメントに異常があります。");

                if (urlText.Split('/').Length == 9)
                    urlText = urlText.Substring(0, argSeg) + "/$SIZE_SEGMENT" + urlText.Substring(fileSeg);
                else
                    urlText = urlText.Substring(0, fileSeg) + "/$SIZE_SEGMENT" + urlText.Substring(fileSeg);
            }
            return urlText;
        }
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
        static Task<string> PostStringAsync(HttpClient client, Uri requestUrl, Dictionary<string, string> content, System.Threading.CancellationToken? token = null)
        { return PostStringAsync(client, requestUrl, new FormUrlEncodedContent(content), token); }
        static async Task<string> PostStringAsync(HttpClient client, Uri requestUrl, HttpContent content, System.Threading.CancellationToken? token = null)
        {
            try
            {
                var res = await client.PostAsync(requestUrl, content, token ?? System.Threading.CancellationToken.None);
                if (res.IsSuccessStatusCode)
                    return await res.Content.ReadAsStringAsync();
                else
                {
                    switch (res.StatusCode)
                    {
                        case HttpStatusCode.Forbidden:
                            throw new ApiErrorException("G+API参照に失敗。ログインセッションが失効しています。" + res.StatusCode, ErrorType.SessionError, requestUrl, content, null);
                        case HttpStatusCode.NotFound:
                            throw new ApiErrorException("G+API参照に失敗。参照された内容は存在しません。" + res.StatusCode, ErrorType.ParameterError, requestUrl, content, null);
                        default:
                            throw new ApiErrorException("G+API参照に失敗。G+側に何らかの障害が発生しています。" + res.StatusCode, ErrorType.UnknownError, requestUrl, content, null);
                    }
                }
            }
            catch (HttpRequestException e)
            {
                if (e.InnerException is WebException)
                    throw new ApiErrorException("G+API参照に失敗。通信中に接続が切断されました。", ErrorType.NetworkError, requestUrl, content, e);
                else
                    throw new ApiErrorException("G+API参照に失敗。想定していないエラーが発生しました。", ErrorType.NetworkError, requestUrl, content, e);
            }
        }
        static async Task<string> GetStringAsync(HttpClient client, Uri requestUrl, System.Threading.CancellationToken? token = null)
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
                            throw new ApiErrorException("G+API参照に失敗。ログインセッションが失効しています。", ErrorType.SessionError, requestUrl, null, null);
                        case HttpStatusCode.NotFound:
                            throw new ApiErrorException("G+API参照に失敗。参照された内容は存在しません。", ErrorType.ParameterError, requestUrl, null, null);
                        default:
                            throw new ApiErrorException("G+API参照に失敗。G+側に何らかの障害が発生しています。", ErrorType.UnknownError, requestUrl, null, null);
                    }
                }
            }
            catch (HttpRequestException e)
            {
                if (e.InnerException is WebException)
                    throw new ApiErrorException("G+API参照に失敗。通信中に接続が切断されました。", ErrorType.NetworkError, requestUrl, null, e);
                else
                    throw new ApiErrorException("G+API参照に失敗。想定していないエラーが発生しました。", ErrorType.NetworkError, requestUrl, null, e);
            }
        }
        static async Task<System.IO.Stream> GetStreamAsync(HttpClient client, Uri requestUrl, System.Threading.CancellationToken? token = null)
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
                            throw new ApiErrorException("G+API参照に失敗。ログインセッションが失効しています。", ErrorType.SessionError, requestUrl, null, null);
                        case HttpStatusCode.NotFound:
                            throw new ApiErrorException("G+API参照に失敗。参照された内容は存在しません。", ErrorType.ParameterError, requestUrl, null, null);
                        default:
                            throw new ApiErrorException("G+API参照に失敗。G+側に何らかの障害が発生しています。", ErrorType.UnknownError, requestUrl, null, null);
                    }
                }
            }
            catch (HttpRequestException e)
            {
                if (e.InnerException is WebException)
                    throw new ApiErrorException("G+API参照に失敗。通信中に接続が切断されました。", ErrorType.NetworkError, requestUrl, null, e);
                else
                    throw new ApiErrorException("G+API参照に失敗。想定していないエラーが発生しました。", ErrorType.NetworkError, requestUrl, null, e);
            }
        }
        enum NodeType { DictKey, DictVal, Array, KeyStr, KeyNum, ValNum, ValStr, None, }
        //文字実体参照対照表
        static Dictionary<string, int> entityPairs = new Dictionary<string, int>() {
        #region
            { "quot", 34  }, { "amp", 38  }, { "lt", 60  }, { "gt", 62  },
            { "nbsp", 160  }, { "iexcl", 161  }, { "cent", 162  }, { "pound", 163  },
            { "curren", 164  }, { "yen", 165  }, { "brvbar", 166  }, { "sect", 167  },
            { "uml", 168 }, { "copy", 169 }, { "ordf", 170 }, { "laquo", 171 },
            { "not", 172 }, { "shy", 173 }, { "reg", 174 }, { "macr", 175 },
            { "deg", 176 }, { "plusmn", 177 }, { "sup2", 178 }, { "sup3", 179 },
            { "acute", 180 }, { "micro", 181 }, { "para", 182 }, { "middot", 183 },
            { "cedil", 184 }, { "sup1", 185 }, { "ordm", 186 }, { "raquo", 187 },
            { "frac14", 188 }, { "frac12", 189 }, { "frac34", 190 }, { "iquest", 191 },
            { "Agrave", 192 }, { "Aacute", 193 }, { "Acirc", 194 }, { "Atilde", 195 },
            { "Auml", 196 }, { "Aring", 197 }, { "AElig", 198 }, { "Ccedil", 199 },
            { "Egrave", 200 }, { "Eacute", 201 }, { "Ecirc", 202 }, { "Euml", 203 },
            { "Igrave", 204 }, { "Iacute", 205 }, { "Icirc", 206 }, { "Iuml", 207 },
            { "ETH", 208 }, { "Ntilde", 209 }, { "Ograve", 210 }, { "Oacute", 211 },
            { "Ocirc", 212 }, { "Otilde", 213 }, { "Ouml", 214 }, { "times", 215 },
            { "Oslash", 216 }, { "Ugrave", 217 }, { "Uacute", 218 }, { "Ucirc", 219 },
            { "Uuml", 220 }, { "Yacute", 221 }, { "THORN", 222 }, { "szlig", 223 },
            { "agrave", 224 }, { "aacute", 225 }, { "acirc", 226 }, { "atilde", 227 },
            { "auml", 228 }, { "aring", 229 }, { "aelig", 230 }, { "ccedil", 231 },
            { "egrave", 232 }, { "eacute", 233 }, { "ecirc", 234 }, { "euml", 235 },
            { "igrave", 236 }, { "iacute", 237 }, { "icirc", 238 }, { "iuml", 239 },
            { "eth", 240 }, { "ntilde", 241 }, { "ograve", 242 }, { "oacute", 243 },
            { "ocirc", 244 }, { "otilde", 245 }, { "ouml", 246 }, { "divide", 247 },
            { "oslash", 248 }, { "ugrave", 249 }, { "uacute", 250 }, { "ucirc", 251 },
            { "uuml", 252 }, { "yacute", 253 }, { "thorn", 254 }, { "yuml", 255 },
            { "OElig", 338 }, { "oelig", 339 }, { "Scaron", 352 }, { "scaron", 353 },
            { "Yuml", 376 }, { "circ", 710 }, { "tilde", 732 }, { "fnof", 402 },
            { "Alpha", 913 }, { "Beta", 914 }, { "Gamma", 915 }, { "Delta", 916 },
            { "Epsilon", 917 }, { "Zeta", 918 }, { "Eta", 919 }, { "Theta", 920 },
            { "Iota", 921 }, { "Kappa", 922 }, { "Lambda", 923 }, { "Mu", 924 },
            { "Nu", 925 }, { "Xi", 926 }, { "Omicron", 927 }, { "Pi", 928 },
            { "Rho", 929 }, { "Sigma", 931 }, { "Tau", 932 }, { "Upsilon", 933 },
            { "Phi", 934 }, { "Chi", 935 }, { "Psi", 936 }, { "Omega", 937 },
            { "alpha", 945 }, { "beta", 946 }, { "gamma", 947 }, { "delta", 948 },
            { "epsilon", 949 }, { "zeta", 950 }, { "eta", 951 }, { "theta", 952 },
            { "iota", 953 }, { "kappa", 954 }, { "lambda", 955 }, { "mu", 956 },
            { "nu", 957 }, { "xi", 958 }, { "omicron", 959 }, { "pi", 960 },
            { "rho", 961 }, { "sigmaf", 962 }, { "sigma", 963 }, { "tau", 964 },
            { "upsilon", 965 }, { "phi", 966 }, { "chi", 967 }, { "psi", 968 },
            { "omega", 969 }, { "thetasym", 977 }, { "upsih", 978 }, { "piv", 982 },
            { "ensp", 8194 }, { "emsp", 8195 }, { "thinsp", 8201 }, { "zwnj", 8204 },
            { "zwj", 8205 }, { "lrm", 8206 }, { "rlm", 8207 }, { "ndash", 8211 },
            { "mdash", 8212 }, { "lsquo", 8216 }, { "rsquo", 8217 }, { "sbquo", 8218 },
            { "ldquo", 8220 }, { "rdquo", 8221 }, { "bdquo", 8222 }, { "dagger", 8224 },
            { "Dagger", 8225 }, { "bull", 8226 }, { "hellip", 8230 }, { "permil", 8240 },
            { "prime", 8242 }, { "Prime", 8243 }, { "lsaquo", 8249 }, { "rsaquo", 8250 },
            { "oline", 8254 }, { "frasl", 8260 }, { "euro", 8364 }, { "image", 8465 },
            { "ewierp", 8472 }, { "real", 8476 }, { "trade", 8482 }, { "alefsym", 8501 },
            { "larr", 8592 }, { "uarr", 8593 }, { "rarr", 8594 }, { "darr", 8595 },
            { "harr", 8596 }, { "crarr", 8629 }, { "lArr", 8656 }, { "uArr", 8657 },
            { "rArr", 8658 }, { "dArr", 8659 }, { "hArr", 8660 }, { "forall", 8704 },
            { "part", 8706 }, { "exist", 8707 }, { "empty", 8709 }, { "nabla", 8711 },
            { "isin", 8712 }, { "notin", 8713 }, { "ni", 8715 }, { "prod", 8719 },
            { "sum", 8721 }, { "minus", 8722 }, { "lowast", 8727 }, { "radic", 8730 },
            { "prop", 8733 }, { "infin", 8734 }, { "ang", 8736 }, { "and", 8743 },
            { "or", 8744 }, { "cap", 8745 }, { "cup", 8746 }, { "int", 8747 },
            { "there4", 8756 }, { "sim", 8764 }, { "cong", 8773 }, { "asymp", 8776 },
            { "ne", 8800 }, { "equiv", 8801 }, { "le", 8804 }, { "ge", 8805 },
            { "sub", 8834 }, { "sup", 8835 }, { "nsub", 8836 }, { "sube", 8838 },
            { "supe", 8839 }, { "oplus", 8853 }, { "otimes", 8855 }, { "perp", 8869 },
            { "sdot", 8901 }, { "lceil", 8968 }, { "rceil", 8969 }, { "lfloor", 8970 },
            { "rfloor", 8971 }, { "lang", 9001 }, { "rang", 9002 }, { "loz", 9674 },
            { "spades", 9824 }, { "clubs", 9827 }, { "hearts", 9829 }, { "diams", 9830 },
        #endregion
        };
    }
    public enum NotificationsFilter { All = -1, Mension = 0, PostIntoYou = 1, OtherPost = 2, CircleIn = 3, Game = 4, TaggedImage = 6, }
    public enum AccountBlockType { Ignore, Block, }
    public enum BlockActionType { Remove, Add, }
    public enum SearchTarget { All = 1, Profile = 2, Activity = 3, Sparks = 4, Hangout = 5 }
    public enum SearchRange { Full = 1, YourCircle = 2, Me = 5 }
    public enum ContentType { Album, Image, Link, InteractiveLink, YouTube, Reshare }

    class OutsideException : Exception
    { public OutsideException(string message, Exception innerException) : base(message, innerException) { } }
}
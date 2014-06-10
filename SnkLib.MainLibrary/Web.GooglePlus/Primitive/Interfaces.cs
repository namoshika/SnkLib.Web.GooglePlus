using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public interface IPlatformClientBuilder
    {
        string Email { get; }
        string Name { get; }
        string IconUrl { get; }
        Task<PlatformClient> Build(IApiAccessor[] accessors = null);
    }
    public interface IPlatformClient
    {
        HttpClient NormalHttpClient { get; }
        HttpClient StreamHttpClient { get; }
        CookieContainer Cookies { get; }
        Uri PlusBaseUrl { get; }
        Uri TalkBaseUrl { get; }
        string AtValue { get; }
        string PvtValue { get; }
        string EjxValue { get; }
    }
    public interface IApiAccessor
    {
        Task<IPlatformClientBuilder[]> GetAccountListAsync(CookieContainer cookies);
        Task<bool> LoginAsync(string email, string password, IPlatformClient client);
        Task<InitData> GetInitDataAsync(IPlatformClient client);
        Task<Tuple<CircleData[], ProfileData[]>> GetCircleDatasAsync(IPlatformClient client);
        Task<ProfileData> GetProfileLiteAsync(string profileId, IPlatformClient client);
        Task<ProfileData> GetProfileFullAsync(string profileId, IPlatformClient client);
        Task<ProfileData> GetProfileAboutMeAsync(IPlatformClient client);
        Task<ProfileData[]> GetFollowingProfilesAsync(string profileId, IPlatformClient client);
        Task<ProfileData[]> GetFollowedProfilesAsync(string profileId, int count, IPlatformClient client);
        Task<ProfileData[]> GetFollowingMeProfilesAsync(IPlatformClient client);
        Task<ProfileData[]> GetIgnoredProfilesAsync(IPlatformClient client);
        Task<ProfileData[]> GetProfileOfPusherAsync(string plusOneId, int pushCount, IPlatformClient client);
        Task<ActivityData> GetActivityAsync(string activityId, IPlatformClient client);
        Task<Tuple<ActivityData[], string>> GetActivitiesAsync(string circleId, string profileId, string ctValue, int length, IPlatformClient client);
        Task<Tuple<NotificationData[], DateTime, string>> GetNotificationsAsync(bool isFetchNewItemMode, int length, string continueToken, IPlatformClient client);
        Task<int> GetUnreadNotificationCountAsync(IPlatformClient client);
        Task<AlbumData> GetAlbumAsync(string albumId, string profileId, IPlatformClient client);
        Task<AlbumData[]> GetAlbumsAsync(string profileId, IPlatformClient client);
        Task<ImageData> GetImageAsync(string imageId, string profileId, IPlatformClient client);
        IObservable<object> GetStreamAttacher(IPlatformClient client);
        Task<ActivityData> PostActivity(string content, Dictionary<string, string> targetCircles, Dictionary<string, string> targetUsers, bool isDisabledComment, bool isDisabledReshare, IPlatformClient client);
        Task<CommentData> PostComment(string activityId, string content, IPlatformClient client);
        Task<CommentData> EditComment(string activityId, string commentId, string content, IPlatformClient client);
        Task DeleteComment(string commentId, IPlatformClient client);
        Task MutateBlockUser(Tuple<string, string>[] userIdAndNames, AccountBlockType blockType, BlockActionType status, IPlatformClient client);
        Task MarkAsReadAsync(NotificationData target, IPlatformClient client);
    }

    /// <summary>APIから取得したデータの解析に問題が生じた時に使用されます。</summary>
    public class InvalidDataException : Exception
    {
        public InvalidDataException(string message, object causeData, Exception innerException)
            : base(message, innerException) { CauseData = causeData; }
        public object CauseData { get; private set; }
    }
    /// <summary>APIが所定の目的を果たせずエラーを返した時に使用されます。</summary>
    public class ApiErrorException : Exception
    {
        public ApiErrorException(string message, ErrorType type, Uri requestUrl, HttpContent requestEntity, HttpResponseMessage response, Exception innerException)
            : base(message, innerException)
        {
            Type = type;
            RequestUrl = requestUrl;
            RequestEntity = requestEntity;
            Response = response;
        }
        public Uri RequestUrl { get; private set; }
        public HttpContent RequestEntity { get; private set; }
        public HttpResponseMessage Response { get; private set; }
        public ErrorType Type { get; private set; }
    }
    public enum ErrorType { ParameterError, SessionError, NetworkError, UnknownError }

    public static class ApiAccessorUtility
    {
        public const string UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/30.0.1599.101 Safari/537.36";
        const string SilhouetteImageUrl = "https://ssl.gstatic.com/s2/profiles/images/silhouette$SIZE_NUM.png";
        const string SilhouetteImagePattern = "https://ssl.gstatic.com/s2/profiles/images/silhouette";
        public static string ComplementUrl(string urlText, string defaultUrl)
        {
            if (string.IsNullOrEmpty(urlText))
                return defaultUrl;
            else
                return urlText.Substring(0, 6) == "https:" ? urlText : "https:" + urlText;
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
        public static string ConvertIntoValidJson(string jsonText)
        {
            jsonText = jsonText.Substring(jsonText.IndexOfAny(new[] { '(', '[', '{' }));
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
}

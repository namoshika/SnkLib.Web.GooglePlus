using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Utility
{
    using SunokoLibrary.Threading;
    using SunokoLibrary.Web.GooglePlus;
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class PlatformClientStub : IPlatformClient
    {
        public PlatformClientStub(System.Net.CookieContainer cookie)
        {
            var handler = new System.Net.Http.HttpClientHandler() { CookieContainer = cookie };
            PlusBaseUrl = new Uri("https://plus.google.com/u/0/");
            TalkBaseUrl = new Uri("https://talkgadget.google.com/u/0/");
            Cookies = cookie;
            NormalHttpClient = new System.Net.Http.HttpClient(handler);
            NormalHttpClient.DefaultRequestHeaders.Add(
                "user-agent", "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/28.0.1500.63 Safari/537.36");
            StreamHttpClient = new System.Net.Http.HttpClient(handler);
            StreamHttpClient.Timeout = TimeSpan.FromMinutes(15);
            StreamHttpClient.DefaultRequestHeaders.Add(
                "user-agent", "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/28.0.1500.63 Safari/537.36");
        }

        public string AtValue { get; set; }
        public string PvtValue { get; set; }
        public string EjxValue { get; set; }
        public Uri PlusBaseUrl { get; private set; }
        public Uri TalkBaseUrl { get; private set; }
        public System.Net.Http.HttpClient NormalHttpClient { get; private set; }
        public System.Net.Http.HttpClient StreamHttpClient { get; private set; }
        public System.Net.CookieContainer Cookies { get; set; }
    }
}

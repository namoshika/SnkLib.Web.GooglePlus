using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Hal.CookieGetterSharp;

namespace SunokoLibrary.Web.GooglePlus
{
    using SunokoLibrary.Web.GooglePlus.Primitive;

#if PLATFORM_WIN
    using System.Runtime.InteropServices;

    public static class PlatformClientFactoryEx
    {
        public static Task<IPlatformClientBuilder[]> ImportFromIE(this PlatformClientFactory factory, IApiAccessor accessor = null)
        {
            return Task.Run(async () =>
                {
                    accessor = accessor ?? new DefaultAccessor();
                    return await accessor.GetAccountListAsync(ImportCookiesFromIE());
                });
        }
        public static Task<IPlatformClientBuilder[]> ImportFrom(this PlatformClientFactory factory, ICookieGetter source, IApiAccessor accessor = null)
        {
            return Task.Run(async () =>
                {
                    accessor = accessor ?? new DefaultAccessor();
                    return await accessor.GetAccountListAsync(ImportCookiesFrom(source));
                });
        }

        public static CookieContainer ImportCookiesFromIE()
        {
            var cookie = new CookieContainer();
            string targetUrl;
            string plusCookie;

            targetUrl = "https://plus.google.com";
            plusCookie = GetProtectedModeIECookieValue(targetUrl).Replace(';', ',');
            cookie.SetCookies(new Uri(targetUrl), plusCookie);

            targetUrl = "https://accounts.google.com";
            plusCookie = GetProtectedModeIECookieValue(targetUrl).Replace(';', ',');
            cookie.SetCookies(new Uri(targetUrl), plusCookie);

            targetUrl = "https://talkgadget.google.com";
            plusCookie = GetProtectedModeIECookieValue(targetUrl).Replace(';', ',');
            cookie.SetCookies(new Uri(targetUrl), plusCookie);

            return cookie;
        }
        public static CookieContainer ImportCookiesFrom(ICookieGetter source)
        {
            var cookie = new CookieContainer();
            Uri targetUrl;
            CookieCollection plusCookie;

            foreach (var target in new[] {
                "https://plus.google.com", "https://accounts.google.com", "https://talkgadget.google.com" })
            {
                targetUrl = new Uri(target);
                plusCookie = source.GetCookieCollection(targetUrl);
                cookie.Add(plusCookie);
            }
            return cookie;
        }

        static string GetIECookieValue(string url)
        {
            int size = 512;
            StringBuilder strBuf = new StringBuilder(size);
            if (InternetGetCookieEx(url, null, strBuf, ref size, INTERNET_COOKIE_HTTPONLY, IntPtr.Zero))
                return strBuf.ToString();
            if (size < 0)
                return null;

            strBuf.Capacity = size;
            if (InternetGetCookieEx(url, null, strBuf, ref size, INTERNET_COOKIE_HTTPONLY, IntPtr.Zero))
                return strBuf.ToString();

            return null;
        }
        static string GetProtectedModeIECookieValue(string url)
        {
            int hResult;
            int size = 512;
            StringBuilder strBuf = new StringBuilder(size);

            do
            {
                hResult = IEGetProtectedModeCookie(url, null, strBuf, ref size, INTERNET_COOKIE_HTTPONLY);
                switch ((uint)hResult)
                {
                    case 0x00000000:
                    case 0x80070103:
                        return strBuf.ToString();
                    case 0x8007007A:
                        //strBufの容量が取得値より足りない場合は容量を増やして再取得する
                        size *= 2;
                        strBuf.Capacity = size;
                        break;
                    default:
                        Marshal.ThrowExceptionForHR(hResult);
                        break;
                }
            }
            while (true);
        }

        const int INTERNET_COOKIE_HTTPONLY = 0x00002000;
        [DllImport("wininet.dll", SetLastError = true)]
        static extern bool InternetGetCookieEx([In]string lpszURL, [In]string lpszCookieName, [In, Out]StringBuilder lpszCookieData, [In, Out]ref int lpdwSize, [In]int dwFlags, [In]IntPtr lpReserved);
        [DllImport("ieframe.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int IEGetProtectedModeCookie([In]String lpszURL, [In]String lpszCookieName, [In, Out]StringBuilder pszCookieData, [In, Out]ref int pcchCookieData, [In]int dwFlags);
    }
#endif
}

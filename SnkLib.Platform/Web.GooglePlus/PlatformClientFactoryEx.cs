using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus
{
    using SunokoLibrary.Web.GooglePlus.Primitive;

#if PLATFORM_WIN
    using System.Runtime.InteropServices;

    public static class PlatformClientFactoryEx
    {
        public static async Task<IPlatformClientBuilder[]> ImportFromChrome(this PlatformClientFactory factory, IApiAccessor accessor = null)
        {
            accessor = accessor ?? new DefaultAccessor();
            return await accessor.GetAccountList(await ImportCookiesFromChrome());
        }
        public static async Task<IPlatformClientBuilder[]> ImportFromIE(this PlatformClientFactory factory, IApiAccessor accessor = null)
        {
            accessor = accessor ?? new DefaultAccessor();
            return await accessor.GetAccountList(ImportCookiesFromIE());
        }

        static readonly string _cookiesFilePath = string.Format(@"{0}\Google\Chrome\User Data\Default\Cookies", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        [Obsolete]
        public static async Task<CookieContainer> ImportCookiesFromChrome()
        {
            var cookies = new CookieContainer();
            try
            {
                using (var dbClient = new System.Data.SQLite.SQLiteConnection(string.Format("Data Source={0}", _cookiesFilePath)))
                {
                    await dbClient.OpenAsync();
                    var command = dbClient.CreateCommand();
                    command.CommandText = "select * from cookies where host_key like \"%.google.com\"";
                    using (var reader = await command.ExecuteReaderAsync())
                        while (reader.Read())
                            cookies.Add(
                                new Cookie()
                                {
                                    Name = (string)reader["name"],
                                    Value = (string)reader["value"],
                                    Path = (string)reader["path"],
                                    Domain = (string)reader["host_key"],
                                    Secure = (long)reader["secure"] == 1,
                                    HttpOnly = (long)reader["httponly"] == 1
                                });
                }
                return cookies;
            }
            catch (System.Data.SQLite.SQLiteException e)
            { throw new FailToOperationException("Chromeからのログイン用Cookie取得に失敗しました。", e); }
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

            if ((hResult = IEGetProtectedModeCookie(url, null, strBuf, ref size, INTERNET_COOKIE_HTTPONLY)) == 0)
                return strBuf.ToString();
            if ((uint)hResult != 0x8007007A)
                Marshal.ThrowExceptionForHR(hResult);

            //strBufの容量が取得値より足りない場合は容量を増やして再取得する
            strBuf.Capacity = size;
            if ((hResult = IEGetProtectedModeCookie(url, null, strBuf, ref size, INTERNET_COOKIE_HTTPONLY)) != 0)
                Marshal.ThrowExceptionForHR(hResult);
            return strBuf.ToString();
        }

        const int INTERNET_COOKIE_HTTPONLY = 0x00002000;
        [DllImport("wininet.dll", SetLastError = true)]
        static extern bool InternetGetCookieEx(string lpszURL, string lpszCookieName, StringBuilder lpszCookieData, ref int lpdwSize, int dwFlags, IntPtr lpReserved);
        [DllImport("ieframe.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern int IEGetProtectedModeCookie(String lpszURL, String lpszCookieName, StringBuilder pszCookieData, ref int pcchCookieData, int dwFlags);
    }
#endif
}

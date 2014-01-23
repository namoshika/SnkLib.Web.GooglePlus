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
    public static class PlatformClientFactoryEx
    {
        public static Task<IPlatformClientBuilder[]> ImportFromChrome(this PlatformClientFactory factory)
        { return ChromeSessionImporter.Import(); }
    }
    public class ChromeSessionImporter : IPlatformClientBuilder
    {
        public ChromeSessionImporter(string email, string name, string iconUrl, int accountIndex, CookieContainer cookies)
        {
            Email = email;
            Name = name;
            IconUrl = iconUrl;
            AccountIndex = accountIndex;
            _cookies = cookies;
        }
        CookieContainer _cookies;

        public int AccountIndex { get; private set; }
        public string Name { get; private set; }
        public string Email { get; private set; }
        public string IconUrl { get; private set; }
        public Task<PlatformClient> Build()
        { return PlatformClient.Factory.Create(_cookies, AccountIndex); }

        static readonly Uri _accountListUrl = new Uri("https://accounts.google.com/b/0/ListAccounts?listPages=0&origin=https%3A%2F%2Fplus.google.com");
        static readonly string _cookiesFilePath = string.Format(@"{0}\Google\Chrome\User Data\Default\Cookies", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
        static readonly System.Text.RegularExpressions.Regex _regex = new System.Text.RegularExpressions.Regex("window.parent.postMessage\\([^\"]*\"(?<jsonTxt>(?:[^\"])*)\"");
        public static async Task<IPlatformClientBuilder[]> Import()
        {
            var cookies = await ImportCookies();
            var hClient = new HttpClient(new HttpClientHandler() { CookieContainer = cookies });
            hClient.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 6.3; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/30.0.1599.101 Safari/537.36");

            try
            {
                var accountListPage = await hClient.GetStringAsync(_accountListUrl);
                var jsonTxt = Uri.UnescapeDataString(_regex.Match(accountListPage).Groups["jsonTxt"].Value.Replace("\\x", "%"));
                if (jsonTxt == string.Empty)
                    return new IPlatformClientBuilder[] { };
                else
                {
                    var json = JArray.Parse(jsonTxt);
                    var generators = json[1]
                        .Select(item => new ChromeSessionImporter((string)item[3], (string)item[2], ApiWrapper.ConvertReplasableUrl((string)item[4]), (int)item[7], cookies))
                        .ToArray();
                    return generators;
                }
            }
            catch (System.Net.Http.HttpRequestException e)
            { throw new FailToOperationException("Chromeでログイン中のアカウントの取得に失敗しました。", e); }
        }
        public static async Task<CookieContainer> ImportCookies()
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
    }
#endif
}

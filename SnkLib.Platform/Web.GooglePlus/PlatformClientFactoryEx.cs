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
        public static async Task<IPlatformClientBuilder[]> ImportFromChrome(this PlatformClientFactory factory, IApiAccessor accessor = null)
        {
            accessor = accessor ?? new DefaultAccessor();
            return await accessor.GetAccountList(await ImportCookiesFromChrome());
        }

        static readonly string _cookiesFilePath = string.Format(@"{0}\Google\Chrome\User Data\Default\Cookies", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
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
    }
#endif
}

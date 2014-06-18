using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Web.GooglePlus
{
    using SunokoLibrary.Web.GooglePlus;
    using SunokoLibrary.Web.GooglePlus.Primitive;
    using SunokoLibrary.Web.GooglePlus.Utility;

    public class ApiWrapperStub : ApiWrapper
    {
        public override Task<System.IO.Stream> GetStreamAsync(System.Net.Http.HttpClient client, Uri requestUrl, System.Threading.CancellationToken? token = null)
        {
            var strm = GetTestResponseFrom("GET", requestUrl, null);
            if(strm == null)
                throw new ApiErrorException("引数requestUrlで指定されたデータが見つかりませんでした。", ErrorType.ParameterError, requestUrl, null, null, null);
            return Task.FromResult(strm);
        }
        public override async Task<string> GetStringAsync(System.Net.Http.HttpClient client, Uri requestUrl, System.Threading.CancellationToken? token = null)
        {
            var strm = await GetStreamAsync(client, requestUrl, token);
            if (strm == null)
                throw new ApiErrorException("引数requestUrlで指定されたデータが見つかりませんでした。", ErrorType.ParameterError, requestUrl, null, null, null);
            using (var reader = new System.IO.StreamReader(strm, Encoding.UTF8))
                return await reader.ReadToEndAsync();
        }
        public override async Task<string> PostStringAsync(System.Net.Http.HttpClient client, Uri requestUrl, Dictionary<string, string> content, System.Threading.CancellationToken? token = null)
        {
            var strm = await GetStreamAsync(client, requestUrl, token);
            if (strm == null)
                throw new ApiErrorException("引数requestUrlで指定されたデータが見つかりませんでした。", ErrorType.ParameterError, requestUrl, null, null, null);
            using (var reader = new System.IO.StreamReader(strm, Encoding.UTF8))
                return await reader.ReadToEndAsync();
        }

        static System.IO.Stream GetTestResponseFrom(string method, Uri requestUrl, Dictionary<string, string> parameters)
        {
            var response = ApiWrapperWithLogger.ResponseLog.Load("TestResponses", method, requestUrl, parameters);
            var strm = new System.IO.MemoryStream(Encoding.UTF8.GetBytes(response.Response));
            return strm;
        }
    }
}

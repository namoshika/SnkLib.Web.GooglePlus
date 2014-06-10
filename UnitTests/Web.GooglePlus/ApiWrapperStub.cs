using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Web.GooglePlus
{
    using SunokoLibrary.Web.GooglePlus;
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class ApiWrapperStub : ApiWrapper
    {
        public override Task<System.IO.Stream> GetStreamAsync(System.Net.Http.HttpClient client, Uri requestUrl, System.Threading.CancellationToken? token = null)
        {
            var strm = GetTestResponseFrom(requestUrl);
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
        public override async Task<string> PostStringAsync(System.Net.Http.HttpClient client, Uri requestUrl, System.Net.Http.HttpContent content, System.Threading.CancellationToken? token = null)
        {
            var strm = await GetStreamAsync(client, requestUrl, token);
            if (strm == null)
                throw new ApiErrorException("引数requestUrlで指定されたデータが見つかりませんでした。", ErrorType.ParameterError, requestUrl, null, null, null);
            using (var reader = new System.IO.StreamReader(strm, Encoding.UTF8))
                return await reader.ReadToEndAsync();
        }

        static System.IO.Stream GetTestResponseFrom(Uri target)
        {
            var testResPath = "TestResponses\\"
                + Uri.EscapeDataString(target.AbsoluteUri).Replace("_", "__").Replace("%", "_") + ".txt";
            return System.IO.File.Exists(testResPath) ? System.IO.File.OpenRead(testResPath) : null;
        }
    }
}

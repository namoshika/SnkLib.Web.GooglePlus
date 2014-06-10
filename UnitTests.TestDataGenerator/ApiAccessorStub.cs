using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.TestDataGenerator
{
    using SunokoLibrary.Web.GooglePlus;
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class ApiAccessorStub : ApiWrapper
    {
        List<string> _paths = new List<string>();
        public override async Task<string> GetStringAsync(System.Net.Http.HttpClient client, Uri requestUrl, System.Threading.CancellationToken? token = null)
        {
            var savePath = Uri.EscapeDataString(requestUrl.AbsoluteUri).Replace("_", "__").Replace('%', '_');
            var responseTxt = await base.GetStringAsync(client, requestUrl, token);
            System.IO.File.WriteAllText(savePath, responseTxt, Encoding.UTF8);
            _paths.Add(savePath);
            return responseTxt;
        }
        public override async Task<string> PostStringAsync(System.Net.Http.HttpClient client, Uri requestUrl, System.Net.Http.HttpContent content, System.Threading.CancellationToken? token = null)
        {
            var savePath = Uri.EscapeDataString(requestUrl.AbsoluteUri).Replace("_", "__").Replace('%', '_');
            var responseTxt = await base.GetStringAsync(client, requestUrl, token);
            System.IO.File.WriteAllText(savePath, responseTxt, Encoding.UTF8);
            _paths.Add(savePath);
            return responseTxt;
        }
        public void ReplaceResponses(Dictionary<string, string> memberNameValues)
        {
            foreach(var file in _paths.Select(path => new { Path = path, Text = System.IO.File.ReadAllText(path) }))
            {
                var txt = file.Text;
                foreach (var item in memberNameValues)
                    txt = txt.Replace(item.Key, item.Value);
                System.IO.File.WriteAllText(file.Path, txt);
            }
        }
    }
}

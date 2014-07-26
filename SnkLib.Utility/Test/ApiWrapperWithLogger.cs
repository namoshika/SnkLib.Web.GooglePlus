using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Utility
{
    using SunokoLibrary.Web.GooglePlus;
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class ApiWrapperWithLogger : ApiWrapper
    {
        static ApiWrapperWithLogger()
        {
            exeDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            logDir = new System.IO.DirectoryInfo(exeDir + "\\NetworkLogs");
            if (logDir.Exists == false)
                logDir.Create();
            else
                foreach (var item in logDir.EnumerateFiles())
                    item.Delete();
        }
        readonly static string exeDir;
        readonly static System.IO.DirectoryInfo logDir;

        System.Collections.Concurrent.ConcurrentQueue<string> _paths = new System.Collections.Concurrent.ConcurrentQueue<string>();
        public override async Task<string> GetStringAsync(System.Net.Http.HttpClient client, Uri requestUrl, System.Threading.CancellationToken? token = null)
        {
            var responseTxt = await base.GetStringAsync(client, requestUrl, token);
            var tsk = Task.Run(async () => _paths.Enqueue(await ResponseLog.Save("GET", requestUrl, null, responseTxt)));
            return responseTxt;
        }
        public override async Task<string> PostStringAsync(System.Net.Http.HttpClient client, Uri requestUrl, Dictionary<string, string> content, System.Threading.CancellationToken? token = null)
        {
            var responseTxt = await base.PostStringAsync(client, requestUrl, content, token);
            var tsk = Task.Run(async () => _paths.Enqueue(await ResponseLog.Save("POST", requestUrl, content, responseTxt)));
            return responseTxt;
        }
        public override async Task<System.IO.Stream> GetStreamAsync(System.Net.Http.HttpClient client, Uri requestUrl, System.Threading.CancellationToken? token = null)
        { return new InterceptStream(await base.GetStreamAsync(client, requestUrl, token), "GET", requestUrl, null); }
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
        [DataContract]
        public class ResponseLog
        {
            readonly static DataContractSerializer _seri
                = new DataContractSerializer(typeof(ResponseLog));
            readonly static System.Security.Cryptography.MD5 _hashGen
                = new System.Security.Cryptography.MD5CryptoServiceProvider();

            [DataMember]
            public string Method { get; set; }
            [DataMember]
            public Uri RequestUrl { get; set; }
            [DataMember]
            public Dictionary<string, string> Parameters { get; set; }
            [DataMember]
            public string Response { get; set; }
            public static Task<string> Save(string method, Uri requestUrl, Dictionary<string, string> parameters, string response)
            {
                return Task.Run(() =>
                    {
                        var logObj = new ResponseLog()
                        {
                            Method = method,
                            RequestUrl = requestUrl,
                            Parameters = parameters,
                            Response = response,
                        };
                        if (logDir.Exists == false)
                            logDir.Create();
                        var path = string.Format("{0}\\{1}.xml", logDir.FullName, GetHash(logObj.Method, logObj.RequestUrl, logObj.Parameters));
                        using (var fileStrm = new System.IO.FileStream(path, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                            _seri.WriteObject(fileStrm, logObj);
                        return path;
                    });
            }
            public static ResponseLog Load(string directoryPath, string method, Uri requestUrl, Dictionary<string, string> parameters)
            {
                var exeDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                var logDir = new System.IO.DirectoryInfo(exeDir + "\\" + directoryPath);
                var path = string.Format("{0}\\{1}.xml", logDir.FullName, GetHash(method, requestUrl, parameters));
                if (System.IO.File.Exists(path) == false)
                    return null;

                using (var fileStrm = new System.IO.FileStream(string.Format("NetworkLogs\\{0}.xml", path), System.IO.FileMode.Open, System.IO.FileAccess.Read))
                    return (ResponseLog)_seri.ReadObject(fileStrm);
            }
            public static string GetHash(string method, Uri requestUrl, Dictionary<string, string> paras)
            {
                paras = paras ?? new Dictionary<string, string>();
                var idTxt = string.Format(
                    "{0} {1} {2}", method, requestUrl.AbsoluteUri,
                    string.Join("&", paras.Select(pair => string.Format("{0}={1}", Uri.EscapeDataString(pair.Key), Uri.EscapeDataString(pair.Value)))));
                var idBytes = Encoding.UTF8.GetBytes(idTxt);
                var idHash = string.Join(string.Empty, _hashGen.ComputeHash(idBytes).Select(byt => string.Format("{0:X}", byt)).ToArray());
                return idHash;
            }
        }
        class InterceptStream : System.IO.Stream
        {
            public InterceptStream(System.IO.Stream baseStream, string method, Uri requestUrl, Dictionary<string, string> parameters)
            {
                _baseStream = baseStream;
                _method = method;
                _requestUrl = requestUrl;
                _parameters = parameters;
                _tmpFilePath = System.IO.Path.GetTempFileName();
                _tmpStream = new System.IO.FileStream(
                    _tmpFilePath, System.IO.FileMode.Create, System.IO.FileAccess.ReadWrite);
            }
            readonly string _tmpFilePath;
            readonly string _method;
            readonly Uri _requestUrl;
            readonly Dictionary<string, string> _parameters;
            readonly System.IO.Stream _baseStream;
            readonly System.IO.Stream _tmpStream;
            bool _disposed = false;

            public override bool CanRead { get { return _baseStream.CanRead; } }
            public override bool CanSeek { get { return _baseStream.CanSeek; } }
            public override bool CanWrite { get { return _baseStream.CanWrite; } }
            public override long Length { get { return _baseStream.Length; } }
            public override long Position
            {
                get { return _baseStream.Position; }
                set { _baseStream.Position = value; }
            }

            public override void Flush() { _baseStream.Flush(); }
            public override int Read(byte[] buffer, int offset, int count)
            {
                var len = _baseStream.Read(buffer, offset, count);
                _tmpStream.Write(buffer, 0, len);
                return len;
            }
            public override long Seek(long offset, System.IO.SeekOrigin origin) { return _baseStream.Seek(offset, origin); }
            public override void SetLength(long value) { _baseStream.SetLength(value); }
            public override void Write(byte[] buffer, int offset, int count) { _baseStream.Write(buffer, offset, count); }
            protected override void Dispose(bool disposing)
            {
                if (_disposed)
                    return;

                base.Dispose(disposing);
                _baseStream.Dispose();
                _tmpStream.Flush();
                _tmpStream.Position = 0;
                using (var reader = new System.IO.StreamReader(_tmpStream))
                    ResponseLog.Save(_method, _requestUrl, _parameters, reader.ReadToEnd());
                _tmpStream.Dispose();
                System.IO.File.Delete(_tmpFilePath);
                _disposed = true;
            }
        }
    }
}

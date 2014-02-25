using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus
{
    using SunokoLibrary.Threading;
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class ImageInfo : AccessorBase
    {
        public ImageInfo(PlatformClient client, ImageData data)
            : base(client)
        {
            _data = data;
            _isolateActivity = LoadedApiTypes >= ImageUpdateApiFlag.LightBox
                ? Client.Activity.InternalGetAndUpdateActivity(_data.IsolateActivity) : null;
        }
        ActivityInfo _isolateActivity;
        ImageData _data;
        readonly AsyncLocker _syncerUpdateLightBox = new AsyncLocker();

        public ImageUpdateApiFlag LoadedApiTypes { get { return _data.LoadedApiTypes; } }
        public string Id { get { return _data.Id; } }
        public string Name { get { return CheckFlag(_data.Name, ImageUpdateApiFlag.Base); } }
        public int Width { get { return CheckFlag(_data.Width, ImageUpdateApiFlag.Base).Value; } }
        public int Height { get { return CheckFlag(_data.Height, ImageUpdateApiFlag.Base).Value; } }
        public string ImageUrl { get { return CheckFlag(_data.ImageUrl, ImageUpdateApiFlag.Base); } }
        public Uri LinkUrl { get { return CheckFlag(_data.LinkUrl, ImageUpdateApiFlag.Base); } }
        public ProfileInfo Owner { get { return Client.People.InternalGetAndUpdateProfile(CheckFlag(_data.Owner, ImageUpdateApiFlag.Base)); } }
        public DateTime CreateDate { get { return CheckFlag(_data.CreateDate, ImageUpdateApiFlag.LightBox).Value; } }
        public ActivityInfo IsolateActivity { get { return CheckFlag(_isolateActivity, ImageUpdateApiFlag.LightBox); } }
        public ImageTagData[] Tags { get { return CheckFlag(_data.AttachedTags, ImageUpdateApiFlag.LightBox); } }

        public async Task UpdateLightBoxAsync(bool isForced)
        {
            await _syncerUpdateLightBox.LockAsync(isForced, () => LoadedApiTypes < ImageUpdateApiFlag.LightBox,
                async () =>
                {
                    try
                    {
                        _data = _data + await Client.ServiceApi.GetImageAsync(_data.Id, _data.Owner.Id, Client);
                        _isolateActivity = _data.LoadedApiTypes >= ImageUpdateApiFlag.LightBox
                            ? Client.Activity.InternalGetAndUpdateActivity(_data.IsolateActivity) : null;
                    }
                    catch (ApiErrorException e) { throw new FailToOperationException("UpdateLightBoxAsync()に失敗。G+API呼び出しで例外が発生しました。", e); }

                }, null);
        }
        //重複対策関数
        T CheckFlag<T>(T target, ImageUpdateApiFlag flag)
        { return CheckFlag(target, "LoadedApiTypes", () => (_data.LoadedApiTypes & flag) == flag, string.Format("{0}フラグを満たさない", flag)); }
    }
}

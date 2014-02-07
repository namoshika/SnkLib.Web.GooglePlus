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
        public string Name { get { return CheckFlag(_data.Name, "LoadedApiTypes", () => LoadedApiTypes >= ImageUpdateApiFlag.Base, "trueでない"); } }
        public int Width { get { return CheckFlag(_data.Width, "LoadedApiTypes", () => LoadedApiTypes >= ImageUpdateApiFlag.Base, "trueでない").Value; } }
        public int Height { get { return CheckFlag(_data.Height, "LoadedApiTypes", () => LoadedApiTypes >= ImageUpdateApiFlag.Base, "trueでない").Value; } }
        public string ImageUrl { get { return CheckFlag(_data.ImageUrl, "LoadedApiTypes", () => LoadedApiTypes >= ImageUpdateApiFlag.Base, "trueでない"); } }
        public ProfileInfo Owner { get { return Client.People.InternalGetAndUpdateProfile(CheckFlag(_data.Owner, "IsUpdatedLightBox", () => LoadedApiTypes >= ImageUpdateApiFlag.Base, "trueでない")); } }
        public DateTime CreateDate { get { return CheckFlag(_data.CreateDate, "LoadedApiTypes", () => LoadedApiTypes >= ImageUpdateApiFlag.LightBox, "trueでない").Value; } }
        public ActivityInfo IsolateActivity { get { return CheckFlag(_isolateActivity, "LoadedApiTypes", () => LoadedApiTypes >= ImageUpdateApiFlag.LightBox, "trueでない"); } }
        public ImageTagData[] Tags { get { return CheckFlag(_data.AttachedTags, "LoadedApiTypes", () => LoadedApiTypes >= ImageUpdateApiFlag.LightBox, "trueでない"); } }

        public async Task UpdateLightBoxAsync(bool isForced, TimeSpan? intervalRestriction = null)
        {
            await _syncerUpdateLightBox.LockAsync(isForced, () => LoadedApiTypes < ImageUpdateApiFlag.LightBox, intervalRestriction,
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
    }
}

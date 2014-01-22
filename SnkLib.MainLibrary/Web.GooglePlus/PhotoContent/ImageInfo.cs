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
            _isolateActivity = data.IsUpdatedLightBox
                ? Client.Activity.InternalGetAndUpdateActivity(_data.IsolateActivity) : null;
        }
        ActivityInfo _isolateActivity;
        ImageData _data;
        readonly AsyncLocker _syncerUpdateLightBox = new AsyncLocker();

        public bool IsUpdatedLightBox { get { return _data.IsUpdatedLightBox; } }
        public string Id { get { return CheckFlag(_data.Id, "IsUpdatedLightBox", () => IsUpdatedLightBox, "trueでない"); } }
        public string Name { get { return CheckFlag(_data.Name, "IsUpdatedLightBox", () => IsUpdatedLightBox, "trueでない"); } }
        public int Width { get { return CheckFlag(_data.Width, "IsUpdatedLightBox", () => IsUpdatedLightBox, "trueでない").Value; } }
        public int Height { get { return CheckFlag(_data.Height, "IsUpdatedLightBox", () => IsUpdatedLightBox, "trueでない").Value; } }
        public string ImageUrl { get { return CheckFlag(_data.ImageUrl, "IsUpdatedLightBox", () => IsUpdatedLightBox, "trueでない"); } }
        public ProfileInfo Owner { get { return Client.People.InternalGetAndUpdateProfile(CheckFlag(_data.Owner, "IsUpdatedLightBox", () => IsUpdatedLightBox, "trueでない")); } }
        public DateTime CreateDate { get { return CheckFlag(_data.CreateDate, "IsUpdatedLightBox", () => IsUpdatedLightBox, "trueでない").Value; } }
        public ActivityInfo IsolateActivity { get { return CheckFlag(_isolateActivity, "IsUpdatedLightBox", () => IsUpdatedLightBox, "trueでない"); } }
        public ImageTagData[] Tags { get { return CheckFlag(_data.AttachedTags, "IsUpdatedLightBox", () => IsUpdatedLightBox, "trueでない"); } }

        public async Task UpdateLightBoxAsync(bool isForced, TimeSpan? intervalRestriction = null)
        {
            await _syncerUpdateLightBox.LockAsync(isForced, () => IsUpdatedLightBox == false, intervalRestriction,
                async () =>
                {
                    try
                    {
                        _data = _data + await Client.ServiceApi.GetImageAsync(_data.Id, _data.Owner.Id, Client);
                        _isolateActivity = _data.IsUpdatedLightBox
                            ? Client.Activity.InternalGetAndUpdateActivity(_data.IsolateActivity) : null;
                    }
                    catch (ApiErrorException e) { throw new FailToOperationException("UpdateLightBoxAsync()に失敗。G+API呼び出しで例外が発生しました。", e); }

                }, null);
        }
    }
}

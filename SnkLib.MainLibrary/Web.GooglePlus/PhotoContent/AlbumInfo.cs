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

    public class AlbumInfo : AccessorBase
    {
        public AlbumInfo(PlatformClient client, AlbumData data)
            : base(client)
        {
            _data = data;
            _attachedActivity = _data.AttachedActivity != null ? client.Activity.GetActivityInfo(_data.AttachedActivity) : null;
            _owner = data.Owner != null ? client.People.InternalGetAndUpdateProfile(data.Owner) : null;
        }

        AlbumData _data;
        ProfileInfo _owner;
        ActivityInfo _attachedActivity;
        readonly AsyncLocker _syncerUpdateAlbum = new AsyncLocker();
        readonly AsyncLocker _syncerUpdateAlbumComments = new AsyncLocker();
        //return CheckFlag(target, "LoadedApiTypes", () => (_data.LoadedApiTypes & flag) == flag, string.Format("{0}フラグを満たさない", flag));
        public AlbumUpdateApiFlag LoadedApiTypes { get { return _data.LoadedApiTypes; } }
        public string Id { get { return _data.Id; } }
        public string Name { get { return CheckFlag(_data.Name, "IsUpdatedAlbum", () => (_data.LoadedApiTypes & AlbumUpdateApiFlag.Base) == AlbumUpdateApiFlag.Base, "AlbumUpdateApiFlag.Baseフラグを満たさない"); } }
        public Uri AlbumUrl { get { return CheckFlag(_data.AlbumUrl, "IsUpdatedAlbum", () => (_data.LoadedApiTypes & AlbumUpdateApiFlag.Base) == AlbumUpdateApiFlag.Base, "AlbumUpdateApiFlag.Baseフラグを満たさない"); } }
        public DateTime CreateDate { get { return CheckFlag(_data.CreateDate, "IsUpdatedAlbum", () => (_data.LoadedApiTypes & AlbumUpdateApiFlag.Full) == AlbumUpdateApiFlag.Full, "AlbumUpdateApiFlag.Fullフラグを満たさない").Value; } }
        public ImageInfo[] BookCovers { get { return CheckFlag(_data.BookCovers, "IsUpdatedAlbum", () => (_data.LoadedApiTypes & AlbumUpdateApiFlag.Full) == AlbumUpdateApiFlag.Full, "AlbumUpdateApiFlag.Fullフラグを満たさない").Select(dt => new ImageInfo(Client, dt)).ToArray(); } }
        public ImageInfo[] Images { get { return CheckFlag(_data.Images, "IsUpdatedAlbum", () => (_data.LoadedApiTypes & AlbumUpdateApiFlag.Full) == AlbumUpdateApiFlag.Full, "AlbumUpdateApiFlag.Fullフラグを満たさない").Select(dt => new ImageInfo(Client, dt)).ToArray(); } }
        public ActivityInfo AttachedActivity { get { return CheckFlag(_attachedActivity, "IsUpdatedAlbumComments", () => (_data.LoadedApiTypes & AlbumUpdateApiFlag.Full) == AlbumUpdateApiFlag.Full, "trueでない"); } }
        public ProfileInfo Owner { get { return CheckFlag(_owner, "IsUpdatedAlbum", () => (_data.LoadedApiTypes & AlbumUpdateApiFlag.Base) == AlbumUpdateApiFlag.Base, "AlbumUpdateApiFlag.Baseフラグを満たさない"); } }

        public async Task UpdateAlbumAsync(bool isForced, TimeSpan? intervalRestriction = null)
        {
            intervalRestriction = intervalRestriction ?? TimeSpan.FromSeconds(1);
            await _syncerUpdateAlbum.LockAsync(
                isForced, () => LoadedApiTypes == AlbumUpdateApiFlag.Unloaded, intervalRestriction,
                async () =>
                {
                    try
                    {
                        _data = _data + await Client.ServiceApi.GetAlbumAsync(Id, Owner.Id, Client);
                        _attachedActivity = Client.Activity.GetActivityInfo(_data.AttachedActivity);
                        _owner = Client.People.InternalGetAndUpdateProfile(_data.Owner);
                    }
                    catch (Primitive.ApiErrorException e)
                    { throw new FailToOperationException("UpdateAlbumAsync()に失敗しました。", e); }

                }, null);
        }
        //public async Task UpdateAlbumComments(bool isForced, TimeSpan? intervalRestriction = null)
        //{
        //    intervalRestriction = intervalRestriction ?? TimeSpan.FromSeconds(1);
        //    using (var releaser = await _syncerUpdateAlbumComments.ReaderLockAsync())
        //    {
        //        if (isForced == false && IsUpdatedAlbumComments || DateTime.UtcNow - _lastUpdateAlbumCommentsDate < intervalRestriction)
        //            return;
        //        using (await releaser.Upgrade())
        //        {
        //            if (isForced == false && IsUpdatedAlbumComments || DateTime.UtcNow - _lastUpdateAlbumCommentsDate < intervalRestriction)
        //                return;
        //            try
        //            {
        //                var json = (await ApiWrapper.ConnectToPhotosAlbumComments(Client.NormalHttpClient, Client.PlusBaseUrl, Owner.Id, Id))[0][1][11][0];
        //                _attachedActivity = new ActivityInfo(Client, id: (string)json[8]);
        //                _attachedActivity.Parse(json: json, isParseComments: false, updaterTypes: ActivityUpdateApiFlag.GetActivities);
        //                _lastUpdateAlbumCommentsDate = DateTime.UtcNow;
        //                IsUpdatedAlbumComments = true;
        //            }
        //            catch (ApiErrorException e)
        //            { throw new FailToOperationException("UpdateAlbumComments()に失敗しました。", e); }
        //        }
        //    }
        //}
    }
}

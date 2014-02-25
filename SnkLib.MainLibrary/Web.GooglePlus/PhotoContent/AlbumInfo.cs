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
            _owner = client.People.InternalGetAndUpdateProfile(data.Owner);
        }
        AlbumData _data;
        ProfileInfo _owner;
        ActivityInfo _attachedActivity;
        readonly AsyncLocker _syncerUpdateAlbum = new AsyncLocker();
        readonly AsyncLocker _syncerUpdateAlbumComments = new AsyncLocker();
        
        public AlbumUpdateApiFlag LoadedApiTypes { get { return _data.LoadedApiTypes; } }
        public string Id { get { return _data.Id; } }
        public string Name { get { return CheckFlag(_data.Name, AlbumUpdateApiFlag.Base); } }
        public Uri AlbumUrl { get { return CheckFlag(_data.AlbumUrl, AlbumUpdateApiFlag.Base); } }
        public DateTime CreateDate { get { return CheckFlag(_data.CreateDate, AlbumUpdateApiFlag.Full).Value; } }
        public ImageInfo[] BookCovers { get { return CheckFlag(_data.BookCovers, AlbumUpdateApiFlag.Albums).Select(dt => new ImageInfo(Client, dt)).ToArray(); } }
        public ImageInfo[] Images { get { return CheckFlag(_data.Images, AlbumUpdateApiFlag.Full).Select(dt => new ImageInfo(Client, dt)).ToArray(); } }
        public ActivityInfo AttachedActivity { get { return CheckFlag(_attachedActivity, AlbumUpdateApiFlag.Full); } }
        public ProfileInfo Owner { get { return _owner; } }

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
                        _attachedActivity = _data.AttachedActivity != null ? Client.Activity.GetActivityInfo(_data.AttachedActivity) : null;
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

        //重複対策関数
        T CheckFlag<T>(T target, AlbumUpdateApiFlag flag)
        { return CheckFlag(target, "LoadedApiTypes", () => (_data.LoadedApiTypes & flag) == flag, string.Format("{0}フラグを満たさない", flag)); }
    }
}

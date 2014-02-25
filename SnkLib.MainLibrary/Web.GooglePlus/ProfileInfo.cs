using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus
{
    using SunokoLibrary.Web.GooglePlus.Primitive;
    using SunokoLibrary.Threading;

    [System.Diagnostics.DebuggerDisplay("{Id,nq}({_data.Name}), {_data.Status}")]
    public class ProfileInfo : AccessorBase
    {
        public ProfileInfo(PlatformClient client, ProfileData data)
            : base(client) { _data = data; }
        ProfileData _data;

        public string Id { get { return _data.Id; } }
        public ProfileUpdateApiFlag LoadedApiTypes { get { return _data.LoadedApiTypes; } }
        public AccountStatus Status
        {
            get
            {
                if (_data.Status == null)
                    throw new InvalidOperationException("LoadedApiTypesの状態がBase未満の状態で参照する事はできません。");
                return _data.Status.Value;
            }
        }
        public string Name { get { return CheckFlag(_data.Name, ProfileUpdateApiFlag.Base); } }
        public string FirstName { get { return CheckFlag(_data.FirstName, ProfileUpdateApiFlag.ProfileGet); } }
        public string LastName { get { return CheckFlag(_data.LastName, ProfileUpdateApiFlag.ProfileGet); } }
        public string Introduction { get { return CheckFlag(_data.Introduction, ProfileUpdateApiFlag.ProfileGet); } }
        public string BraggingRights { get { return CheckFlag(_data.BraggingRights, ProfileUpdateApiFlag.ProfileGet); } }
        public string Occupation { get { return CheckFlag(_data.Occupation, ProfileUpdateApiFlag.ProfileGet); } }
        public string GreetingText { get { return CheckFlag(_data.GreetingText, ProfileUpdateApiFlag.Base); } }
        public string NickName { get { return CheckFlag(_data.NickName, ProfileUpdateApiFlag.ProfileGet); } }
        public string IconImageUrl { get { return CheckFlag(_data.IconImageUrl, ProfileUpdateApiFlag.Base); } }
        public RelationType Relationship { get { return CheckFlag(_data.Relationship, ProfileUpdateApiFlag.ProfileGet).Value; } }
        public GenderType GenderType { get { return CheckFlag(_data.GenderType, ProfileUpdateApiFlag.ProfileGet).Value; } }
        public LookingFor LookingFor { get { return CheckFlag(_data.LookingFor, ProfileUpdateApiFlag.ProfileGet); } }
        public ReadOnlyCollection<EmploymentInfo> Employments
        { get { return new ReadOnlyCollection<EmploymentInfo>(CheckFlag(_data.Employments, ProfileUpdateApiFlag.ProfileGet)); } }
        public ReadOnlyCollection<EducationInfo> Educations
        { get { return new ReadOnlyCollection<EducationInfo>(CheckFlag(_data.Educations, ProfileUpdateApiFlag.ProfileGet)); } }
        public ReadOnlyCollection<ContactInfo> ContactsInHome
        { get { return new ReadOnlyCollection<ContactInfo>(CheckFlag(_data.ContactsInHome, ProfileUpdateApiFlag.ProfileGet)); } }
        public ReadOnlyCollection<ContactInfo> ContactsInWork
        { get { return new ReadOnlyCollection<ContactInfo>(CheckFlag(_data.ContactsInWork, ProfileUpdateApiFlag.ProfileGet)); } }
        public ReadOnlyCollection<UrlInfo> OtherProfileUrls
        { get { return new ReadOnlyCollection<UrlInfo>(CheckFlag(_data.OtherProfileUrls, ProfileUpdateApiFlag.ProfileGet)); } }
        public ReadOnlyCollection<UrlInfo> ContributeUrls
        { get { return new ReadOnlyCollection<UrlInfo>(CheckFlag(_data.ContributeUrls, ProfileUpdateApiFlag.ProfileGet)); } }
        public ReadOnlyCollection<UrlInfo> RecommendedUrls
        { get { return new ReadOnlyCollection<UrlInfo>(CheckFlag(_data.RecommendedUrls, ProfileUpdateApiFlag.ProfileGet)); } }
        public ReadOnlyCollection<string> PlacesLived
        { get { return new ReadOnlyCollection<string>(CheckFlag(_data.PlacesLived, ProfileUpdateApiFlag.ProfileGet)); } }
        public ReadOnlyCollection<string> OtherNames
        { get { return new ReadOnlyCollection<string>(CheckFlag(_data.OtherNames, ProfileUpdateApiFlag.ProfileGet)); } }
        public ReadOnlyCollection<CircleInfo> Circles
        {
            get
            {
                if (Client.People.CirclesAndBlockStatus != CircleUpdateLevel.LoadedWithMembers)
                    throw new InvalidOperationException("サークル情報が初期化されていません。UpdateLookupCircleAsync()を呼び出して初期化してください。");
                return new ReadOnlyCollection<CircleInfo>(CheckFlag(
                    Client.People.Circles.Where(inf => inf.ContainsKey(Id)).ToList(), ProfileUpdateApiFlag.LookupCircle));
            }
        }

        public AlbumInfo GetAlbumAsync(string albumId)
        { return new AlbumInfo(Client, new AlbumData(albumId, owner: _data)); }
        public IInfoList<ActivityInfo> GetActivities()
        { return new CircleInfo.ActivityInfoList(null, this, Client, null, null); }
        public async Task<AlbumInfo[]> GetAlbumsAsync()
        {
            try
            {
                var resultAlbums = await Client.ServiceApi.GetAlbumsAsync(_data.Id, Client);
                return resultAlbums.Select(dt => new AlbumInfo(Client, dt)).ToArray();
            }
            catch (ApiErrorException e)
            { throw new FailToOperationException("GetAlbumsAsync()に失敗。G+API呼び出しで例外が発生しました。", e); }
        }
        public async Task<ProfileInfo[]> GetFollowingProfiles()
        {
            await UpdateLookupProfileAsync(false);
            if (Status == AccountStatus.Active)
                try
                {
                    var results = new List<ProfileInfo>();
                    var apiResult = await Client.ServiceApi.GetFollowingProfilesAsync(_data.Id, Client);
                    foreach (var item in apiResult)
                    {
                        Client.People.InternalUpdateProfile(item);
                        results.Add(Client.People.GetProfileOf(item.Id));
                    }
                    return results.ToArray();
                }
                catch (ApiErrorException e)
                { throw new FailToOperationException("GetFollowingProfile()に失敗。G+API呼び出しで例外が発生しました。", e); }
            else
                throw new InvalidOperationException(
                    "GoogleProfileを作成していないユーザーからアルバムを取得する事はできません。");
        }
        public async Task<ProfileInfo[]> GetFollowedProfiles(int count)
        {
            await UpdateLookupProfileAsync(false);
            if (Status == AccountStatus.Active)
                try
                {
                    var results = new List<ProfileInfo>();
                    var json = await Client.ServiceApi.GetFollowedProfilesAsync(_data.Id, count, Client);
                    foreach (var item in json)
                        results.Add(Client.People.InternalGetAndUpdateProfile(item));
                    return results.ToArray();
                }
                catch (ApiErrorException e)
                { throw new FailToOperationException("GetFollowingProfile()に失敗。G+API呼び出しで例外が発生しました。", e); }
            else
                throw new InvalidOperationException(
                    "GoogleProfileを作成していないユーザーからアルバムを取得する事はできません。");
        }
        public async Task UpdateLookupProfileAsync(bool isForced)
        {
            var cache = Client.People.InternalGetProfileCache(_data.Id);
            await cache.SyncerUpdateProfileSummary.LockAsync(
                isForced, () => (cache.Value.LoadedApiTypes & ProfileUpdateApiFlag.LookupProfile) != ProfileUpdateApiFlag.LookupProfile,
                async () =>
                {
                    try
                    {
                        var apiResult = await Client.ServiceApi.GetProfileLiteAsync(_data.Id, Client);
                        _data = Client.People.InternalUpdateProfile(apiResult);
                    }
                    catch (ApiErrorException e)
                    { throw new FailToOperationException("ProfileInfo.UpdateLookupProfileAsync()に失敗。", e); }
                },
                () => _data = cache.Value);
        }
        public async Task UpdateProfileGetAsync(bool isForced)
        {
            var cache = Client.People.InternalGetProfileCache(_data.Id);
            await cache.SyncerUpdateProfileGet.LockAsync(
                isForced, () => (cache.Value.LoadedApiTypes & ProfileUpdateApiFlag.ProfileGet) != ProfileUpdateApiFlag.ProfileGet,
                async () =>
                {
                    try
                    {
                        var apiResult = await Client.ServiceApi.GetProfileFullAsync(_data.Id, Client);
                        _data = Client.People.InternalUpdateProfile(apiResult);
                    }
                    catch (ApiErrorException e)
                    { throw new FailToOperationException("UpdateProfileGetAsync()に失敗。G+API呼び出しで例外が発生しました。", e); }
                },
                () => _data = cache.Value);
        }

        //重複対策関数
        T CheckFlag<T>(T target, ProfileUpdateApiFlag flag)
        {
            if (_data.Status == AccountStatus.MailOnly)
                throw new InvalidOperationException("StatusプロパティがMailOnlyの状態で各プロパティを参照する事はできません。");
            return CheckFlag(target, "LoadedApiTypes", () => (_data.LoadedApiTypes & flag) == flag, string.Format("{0}フラグを満たさない", flag));
        }
    }
    public class ProfileEqualityComparer : IEqualityComparer<ProfileInfo>
    {
        public bool Equals(ProfileInfo x, ProfileInfo y)
        { return x == y || x != null && y != null && x.Id == y.Id; }
        public int GetHashCode(ProfileInfo obj)
        { return obj.Id.GetHashCode(); }

        public static ProfileEqualityComparer Default = new ProfileEqualityComparer();
    }
}

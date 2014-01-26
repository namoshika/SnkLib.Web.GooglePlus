using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;

namespace SunokoLibrary.Web.GooglePlus
{
    using SunokoLibrary.Collection.Generic;
    using SunokoLibrary.Threading;
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class PeopleContainer : AccessorBase
    {
        public PeopleContainer(PlatformClient client)
            : base(client)
        {
            _circles = new CircleInfo[] { };
            _yourCircle = new GooglePlus.YourCircle(client);
            _profileCache = new Collection.Generic.CacheDictionary<string, ProfileCache, ProfileData>(2000, 3, dt => new ProfileCache() { Value = dt });
            _followerCircle = new GroupContainer(client, "Follower");
            _blockCircle = new EditableGroupContainer(
                client, "blocked", null,
                (opType, infos) => ApiWrapper.ConnectToMutateBlockUser(
                    client.NormalHttpClient, client.PlusBaseUrl, infos.Select(a => Tuple.Create(a.Id, a.Name)).ToArray(),
                    Primitive.AccountBlockType.Block, opType, client.AtValue));
            _ignoreCircle = new EditableGroupContainer(
                client, "ignored", null,
                (opType, infos) => ApiWrapper.ConnectToMutateBlockUser(
                    client.NormalHttpClient, client.PlusBaseUrl, infos.Select(a => Tuple.Create(a.Id, a.Name)).ToArray(),
                    Primitive.AccountBlockType.Ignore, opType, client.AtValue));
            CirclesAndBlockStatus = CircleUpdateLevel.Unloaded;
            IsUpdatedIgnore = false;
            PublicCircle = new PostRange(Client, "anyone", "全員");
            ExtendedCircle = new PostRange(Client, "", "");
        }
        readonly AsyncLocker _syncerUpdateCircleAndBlock = new AsyncLocker();
        readonly AsyncLocker _syncerUpdateFollowers = new AsyncLocker();
        readonly AsyncLocker _syncerUpdateIgnore = new AsyncLocker();
        readonly SemaphoreSlim _syncerGetProfileOfMe = new SemaphoreSlim(1, 1);
        CircleInfo[] _circles;
        YourCircle _yourCircle;
        GroupContainer _followerCircle;
        EditableGroupContainer _blockCircle;
        EditableGroupContainer _ignoreCircle;
        ProfileData _profileAboutMe; 
        CacheDictionary<string, ProfileCache, ProfileData> _profileCache;

        public CircleUpdateLevel CirclesAndBlockStatus { get; private set; }
        public bool IsUpdatedFollowers { get; private set; }
        public bool IsUpdatedIgnore { get; private set; }
        public IPostRange PublicCircle { get; private set; }
        public IPostRange ExtendedCircle { get; private set; }
        public EditableGroupContainer BlockList { get { return _blockCircle; } }
        public EditableGroupContainer IgnoreList { get { return _ignoreCircle; } }
        public GroupContainer FollowerList
        { get { return CheckFlag(_followerCircle, "IsUpdatedFollowers", () => IsUpdatedFollowers, "trueでない"); } }
        public YourCircle YourCircle
        { get { return CheckFlag(_yourCircle, "CirclesAndBlockStatus", () => CirclesAndBlockStatus >= CircleUpdateLevel.Loaded, "CircleUpdateLevel.Loaded以上でない"); } }
        public ReadOnlyCollection<CircleInfo> Circles
        { get { return CheckFlag(new ReadOnlyCollection<CircleInfo>(_circles), "CirclesAndBlockStatus", () => CirclesAndBlockStatus >= CircleUpdateLevel.Loaded, "CircleUpdateLevel.Loaded以上でない"); } }

        public ProfileInfo GetProfileOf(string plusId)
        {
            var cache = InternalGetProfileCache(plusId);
            return new ProfileInfo(Client, cache.Value);
        }
        public async Task UpdateCirclesAndBlockAsync(bool isForced, CircleUpdateLevel updateLevel, TimeSpan? intervalRestriction = null)
        {
            await _syncerUpdateCircleAndBlock.LockAsync(isForced, () => CirclesAndBlockStatus < updateLevel, intervalRestriction, async () =>
                {
                    if (Client.IsLoadedHomeInitData == false)
                        throw new FailToOperationException("PlatformClient.IsLoadedHomeInitDataがfalseの状態でUpdate()する事は出来ません。", null);
                    var lastUpdateCirclesAndBlockDate = DateTime.UtcNow;
                    if (updateLevel == CircleUpdateLevel.Loaded)
                    {
                        if (CirclesAndBlockStatus >= updateLevel)
                            return;
                        _yourCircle.Refresh(null, Client.LatestActivities, Client.EjxValue);
                        _circles = Client.Circles.Select(dt => _circles != null
                            ? _circles.FirstOrDefault(info => info.Id == dt.Id) ?? new CircleInfo(Client, dt)
                            : new CircleInfo(Client, dt)).ToArray();
                        CirclesAndBlockStatus = CircleUpdateLevel.Loaded;
                    }
                    else
                    {
                        var lookupedProfiles = new List<ProfileInfo>();
                        CircleData[] circles;
                        ProfileData[] profiles;
                        try
                        {
                            var res = await Client.ServiceApi.GetCircleDatasAsync(Client);
                            circles = res.Item1;
                            profiles = res.Item2;
                        }
                        catch (ApiErrorException e)
                        { throw new FailToOperationException<PeopleContainer>("UpdateCirclesAndBlockAsync()に失敗しました。G+API呼び出しで例外が発生しました。", this, e); }

                        //メンバ変数に収めていく。ブロックサークルは別枠扱い
                        List<CircleInfo> circleInfos = new List<CircleInfo>();
                        foreach (var item in circles)
                            switch (item.Id)
                            {
                                case "15":
                                    _blockCircle.Refresh(item.Members.Select(dt => InternalGetAndUpdateProfile(dt)).ToArray());
                                    break;
                                case "anyone":
                                    _yourCircle.Refresh(
                                        item.Members.Select(dt => InternalGetAndUpdateProfile(dt)).ToArray(),
                                        Client.LatestActivities, Client.EjxValue);
                                    break;
                                default:
                                    var circle = _circles.FirstOrDefault(info => info.Id == item.Id) ?? new CircleInfo(Client, item);
                                    circle.Refresh(item.Members.Select(dt => InternalGetAndUpdateProfile(dt)).ToArray());
                                    circleInfos.Add(circle);
                                    break;
                            }
                        _circles = circleInfos.ToArray();
                        foreach (var item in profiles)
                            InternalUpdateProfile(item);
                        CirclesAndBlockStatus = CircleUpdateLevel.LoadedWithMembers;
                    }
                    OnUpdatedCirclesAndBlock(new EventArgs());
                }, null);
        }
        public async Task UpdateIgnoreAsync(bool isForced, TimeSpan? intervalRestriction = null)
        {
            await _syncerUpdateIgnore.LockAsync(isForced, () => IsUpdatedIgnore == false, intervalRestriction, async () =>
                {
                    try
                    {
                        var json = await Client.ServiceApi.GetIgnoredProfilesAsync(Client);
                        var ignoreLst = new List<ProfileInfo>();
                        foreach (var item in json)
                        {
                            //lookupCircleする前に既に生成していたProfileがある場合は
                            //使いまわすようにしながらProfileインスタンスを生成する
                            var profileId = item.Id;
                            var profile = InternalUpdateProfile(new ProfileData(
                                profileId, item.Name, item.IconImageUrl, loadedApiTypes: ProfileUpdateApiFlag.Base));
                            ignoreLst.Add(InternalGetAndUpdateProfile(profile));
                        }
                        _ignoreCircle.Refresh(ignoreLst.ToArray());
                        IsUpdatedIgnore = true;
                        OnUpdatedIgnore(new EventArgs());
                    }
                    catch (ApiErrorException e)
                    { throw new FailToOperationException<PeopleContainer>("UpdateIgnoreAsync()に失敗。G+API呼び出しで例外が発生しました。", this, e); }
                }, null);
        }
        public async Task UpdateFollowerAsync(bool isForced, TimeSpan? intervalRestriction = null)
        {
            await _syncerUpdateFollowers.LockAsync(isForced, () => IsUpdatedFollowers == false, intervalRestriction, async () =>
                {
                    try
                    {
                        var resultLst = await Client.ServiceApi.GetFollowingMeProfilesAsync(Client);
                        foreach (var item in resultLst)
                            InternalUpdateProfile(item);
                        _followerCircle.Refresh(resultLst.Select(info => InternalGetAndUpdateProfile(info)).ToArray());

                        IsUpdatedFollowers = true;
                        OnUpdatedFollowers(new EventArgs());
                    }
                    catch (ApiErrorException e)
                    { throw new FailToOperationException<PeopleContainer>("UpdateFollowerAsync()に失敗。G+API呼び出しで例外が発生しました。", this, e); }
                }, null);
        }
        public async Task<ProfileInfo> GetProfileOfMeAsync(bool isForced)
        {
            await _syncerGetProfileOfMe.WaitAsync(3000);
            if (isForced == false && _profileAboutMe != null)
                return GetProfileOf(_profileAboutMe.Id);
            else
                try
                {
                    var apiResult = await Client.ServiceApi.GetProfileAboutMeAsync(Client);
                    _profileAboutMe = InternalUpdateProfile(apiResult);
                    return GetProfileOf(_profileAboutMe.Id);
                }
                catch (ApiErrorException e)
                { throw new FailToOperationException<PeopleContainer>("GetProfileOfMeAsync()に失敗。ログインされてるユーザの情報の取得に失敗しました。", this, e); }
        }
        internal ProfileCache InternalGetProfileCache(string targetId)
        {
            ProfileCache result;
            if (_profileCache.TryGetValue(targetId, out result) == false)
                result = _profileCache.Add(targetId, new ProfileData(targetId));
            return result;
        }
        internal ProfileData InternalUpdateProfile(ProfileData newValue)
        { return _profileCache.Update(newValue.Id, newValue).Value; }
        internal ProfileInfo InternalGetAndUpdateProfile(ProfileData newValue)
        { return new ProfileInfo(Client, InternalUpdateProfile(newValue)); }
        
        public event EventHandler UpdatedCirclesAndBlock;
        protected virtual void OnUpdatedCirclesAndBlock(EventArgs e)
        {
            if (UpdatedCirclesAndBlock != null)
                UpdatedCirclesAndBlock(this, e);
        }
        public event EventHandler UpdatedFollowers;
        protected virtual void OnUpdatedFollowers(EventArgs e)
        {
            if (UpdatedFollowers != null)
                UpdatedFollowers(this, e);
        }
        public event EventHandler UpdatedIgnore;
        protected virtual void OnUpdatedIgnore(EventArgs e)
        {
            if (UpdatedIgnore != null)
                UpdatedIgnore(this, e);
        }
    }
    public enum CircleUpdateLevel { Unloaded = 0, Loaded = 1, LoadedWithMembers = 2 }

    public class YourCircle : CircleInfo
    {
        public YourCircle(PlatformClient client) : base(client, new CircleData("anyone", "全員", null)) { }
        ActivityData[] _activities;
        string _ctValue;

        protected internal override void Refresh(ProfileInfo[] members)
        { throw new NotSupportedException("YourCircleクラスはRefresh(IEnumerable<ProfileInfo>)をサポートしていません。"); }
        protected internal virtual void Refresh(ProfileInfo[] members, ActivityData[] activities, string ctValue)
        {
            //HomeInit情報ではmembersがnullになる。この時のために
            //membersがnull時はRefreshを実行しないようにする。
            if (members != null)
                base.Refresh(members);
            _activities = activities;
            _ctValue = ctValue;
        }
        public override IObservable<ActivityInfo> GetStream()
        { return Client.Activity.GetStream().OfType<ActivityInfo>(); }
        public IInfoList<ActivityInfo> GetActivities(bool useCache)
        {
            if (useCache && _ctValue != null)
            {
                var list = new ActivityInfoList(this, null, Client, _activities, _ctValue);
                _activities = null;
                _ctValue = null;
                return list;
            }
            else
                return GetActivities();
        }
    }
    public class PostRange : AccessorBase, IPostRange
    {
        public PostRange(PlatformClient client, string id, string name)
            : base(client)
        {
            Id = id;
            Name = name;
        }
        public string Name { get; private set; }
        public string Id { get; private set; }
        public async virtual Task<bool> Post(string content)
        {
            try
            {
                await ApiWrapper.ConnectToPost(
                    Client.NormalHttpClient, Client.PlusBaseUrl, DateTime.Now, 0, (await Client.People.GetProfileOfMeAsync(false)).Id,
                    new Dictionary<string, string> { { Id, Name } }, new Dictionary<string, string> { },
                    null, content, false, false, Client.AtValue);
                return true;
            }
            catch (ApiErrorException)
            { return false; }
        }
    }
    public class GroupContainer : AccessorBase
    {
        public GroupContainer(PlatformClient client, string name)
            : base(client) { Name = name; }
        HashSet<string> _membersHashSet;
        ProfileInfo[] _protectedMembers;

        public virtual bool IsLoadedMember { get; private set; }
        public virtual string Name { get; private set; }
        protected virtual ProfileInfo[] ProtectedMembers
        {
            get { return _protectedMembers; }
            set
            {
                _protectedMembers = value;
                _membersHashSet = value != null ? new HashSet<string>(value.Select(dt => dt.Id)) : null;
            }
        }

        public virtual IEnumerable<ProfileInfo> GetMembers()
        { return CheckFlag(ProtectedMembers, "IsLoadedMember", () => IsLoadedMember, "trueでない"); }
        public bool ContainsKey(string itemProfileId)
        {
            return CheckFlag(
                _membersHashSet != null ? _membersHashSet.Contains(itemProfileId) : false,
                "IsLoadedMember", () => IsLoadedMember, "trueでない");
        }
        protected internal virtual void Refresh(ProfileInfo[] members)
        {
            ProtectedMembers = members;
            IsLoadedMember = true;
        }
    }
    public class EditableGroupContainer : GroupContainer
    {
        public EditableGroupContainer(PlatformClient client, string name, List<ProfileInfo> members,
            Func<BlockActionType, ProfileInfo[], Task> editProc)
            : base(client, name) { _editorProc = editProc; }

        Func<BlockActionType, ProfileInfo[], Task> _editorProc;
        public async Task EditRange(BlockActionType operationType, params ProfileInfo[] targets)
        {
            await _editorProc(operationType, targets);
            ProtectedMembers = operationType == BlockActionType.Add
                ? ProtectedMembers.Union(targets, ProfileEqualityComparer.Default).ToArray()
                : ProtectedMembers.Where(info => targets.Any(profile => profile.Id == info.Id)).ToArray();
        }
    }
    public class ProfileCache : ICacheInfo<ProfileData>
    {
        public ProfileData Value { get; set; }
        public readonly AsyncLocker SyncerUpdateProfileSummary = new AsyncLocker();
        public readonly AsyncLocker SyncerUpdateCircles = new AsyncLocker();
        public readonly AsyncLocker SyncerUpdateProfileGet = new AsyncLocker();
    }

    public interface IInfoList<T>
    {
        Task<T[]> TakeAsync(int length);
    }
    public interface IReadRange
    {
        IObservable<ActivityInfo> GetStream();
        IInfoList<ActivityInfo> GetActivities();
    }
    public interface IPostRange
    {
        string Id { get; }
        string Name { get; }
        Task<bool> Post(string content);
    }
}

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive.Linq;

namespace SunokoLibrary.Web.GooglePlus
{
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class CircleInfo : GroupContainer, IReadRange, IPostRange
    {
        public CircleInfo(PlatformClient client, CircleData targetData)
            : base(client, targetData.Name)
        {
            if (targetData == null)
                throw new ArgumentNullException("引数targetDataをnullにする事はできません。");
            _data = targetData;
        }
        CircleData _data;

        public string Id { get { return _data.Id; } }
        public virtual IObservable<ActivityInfo> GetStream()
        {
            return Observable.Create<ActivityInfo>(obs =>
                {
                    return Observable.Start(() =>
                        {
                            if (IsLoadedMember == false)
                                Client.Relation.UpdateCirclesAndBlockAsync(
                                    false, CircleUpdateLevel.LoadedWithMembers, TimeSpan.FromSeconds(1)).Wait();
                            return (ActivityInfo)null;
                        })
                        .Where(info => false)
                        .Concat(Client.Activity.GetStream()
                            .OfType<ActivityInfo>()
                            .Where(info => info.PostStatus != PostStatusType.Removed && ContainsKey(info.PostUser.Id)))
                        .Subscribe(obs);
                });
        }
        public virtual IInfoList<ActivityInfo> GetActivities()
        { return new ActivityInfoList(this, null, Client, null, null); }
        public async virtual Task<bool> Post(string content)
        {
            try
            {
                await ApiWrapper.ConnectToPost(
                    Client.NormalHttpClient, Client.PlusBaseUrl, DateTime.Now, 0, (await Client.Relation.GetProfileOfMeAsync(false)).Id,
                    new Dictionary<string, string> { { Id, Name } }, new Dictionary<string, string> { },
                    null, content, false, false, Client.AtValue);
                return true;
            }
            catch (ApiErrorException)
            { return false; }
        }

        public class ActivityInfoList : AccessorBase, IInfoList<ActivityInfo>
        {
            public ActivityInfoList(CircleInfo circleInfo, ProfileInfo profileInfo, PlatformClient client, ActivityData[] activities, string ctVal)
                : base(client)
            {
                _circleInfo = circleInfo;
                _profileInfo = profileInfo;
                _activities = activities != null ? new Queue<ActivityData>(activities) : new Queue<ActivityData>();
                _ctValue = ctVal;
            }
            readonly System.Threading.SemaphoreSlim _syncer = new System.Threading.SemaphoreSlim(1, 1);
            string _ctValue;
            CircleInfo _circleInfo;
            ProfileInfo _profileInfo;
            Queue<ActivityData> _activities;

            public async Task<ActivityInfo[]> TakeAsync(int length)
            {
                var activities = new List<ActivityInfo>();
                for (var i = 0; i < length; )
                {
                    if (_activities.Count > 0)
                    {
                        activities.Add(new ActivityInfo(Client, _activities.Dequeue()));
                        i++;
                        continue;
                    }

                    var oneSetSize = Math.Min(length - activities.Count, 40);
                    var oneSetCount = 0;
                    Tuple<ActivityData[], string> apiResult;
                    try
                    {
                        await _syncer.WaitAsync();
                        var circleId = _circleInfo != null ? _circleInfo.Id == "anyone" ? null : _circleInfo.Id : null;
                        var profileId = _profileInfo != null ? _profileInfo.Id : null;
                        apiResult = await Client.ServiceApi.GetActivitiesAsync(circleId, profileId, _ctValue, length, Client);
                        _ctValue = apiResult.Item2;
                    }
                    finally
                    { _syncer.Release(); }

                    foreach (var item in apiResult.Item1)
                    {
                        oneSetCount++;
                        activities.Add(Client.Activity.GetActivityInfo(Client.Activity.InternalUpdateActivity(item).Id));
                        if (++i >= length)
                            break;
                    }
                    //一度にsingleSize分だけ読み込む。同時にsingleLengthで実際何件読み込んだかを記録し、
                    //期待した件数と実際のを比べて違ったらそれ以上読み込めないとして切り上げる
                    if (oneSetCount < oneSetSize)
                        break;
                }
                return activities.ToArray();
            }
        }
    }
    public static class CircleInfoEx
    {
        public static async Task<bool> Post(this IEnumerable<IPostRange> enumerable, string content)
        {
            try
            {
                var groups = CircleInfo.GroupByClient(enumerable);
                foreach (var rangePairs in groups)
                    if (rangePairs.Item1 != null)
                    {
                        var client = rangePairs.Item1;
                        var circles = rangePairs.Item2.Cast<IPostRange>();
                        await ApiWrapper.ConnectToPost(
                            client.NormalHttpClient, client.PlusBaseUrl, DateTime.Now, 0, (await client.Relation.GetProfileOfMeAsync(false)).Id,
                            circles.ToDictionary(obj => obj.Id, obj => obj.Name), new Dictionary<string, string> { },
                            null, content, false, false, client.AtValue);
                    }
                    else
                        foreach (var postRange in rangePairs.Item2)
                            await postRange.Post(content);
                return true;
            }
            catch (ApiErrorException)
            { return false; }
        }
        public static IObservable<ActivityInfo> GetStream(this IEnumerable<IReadRange> enumerable)
        {
            var groups = CircleInfo.GroupByClient(enumerable);
            var streams = groups.Where(pair => pair.Item1 != null).Select(
                aa => aa.Item1.Activity.GetStream()
                    .OfType<ActivityInfo>()
                    .Where(info =>
                    {
                        var res = info.PostStatus != PostStatusType.Removed
                            && info.PostUser.Circles.Where(circle => aa.Item2.Any(val => ((CircleInfo)val).Id == circle.Id)).Any();
                        return res;
                    }))
                    .Concat(groups.Last().Item2.Select(readRange => readRange.GetStream()));
            return Observable.Merge(streams).Distinct(activity => activity.Id);
        }
    }
}

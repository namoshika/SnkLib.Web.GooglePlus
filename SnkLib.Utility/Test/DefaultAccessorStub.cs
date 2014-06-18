using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Utility
{
    using SunokoLibrary.Threading;
    using SunokoLibrary.Web.GooglePlus;
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class DefaultAccessorStub : IApiAccessor, IDataFactoryManager
    {
        public DefaultAccessorStub()
        {
            ProfileFactory = new ProfileDataFactory(this);
            ActivityFactory = new ActivityDataFactory(this);
            CommentFactory = new CommentDataFactory(this);
            AttachedFactory = new AttachedDataFactory(this);
            NotificationFactory = new NotificationDataFactory(this);
        }
        public ProfileDataFactory ProfileFactory { get; private set; }
        public ActivityDataFactory ActivityFactory { get; private set; }
        public CommentDataFactory CommentFactory { get; private set; }
        public AttachedDataFactory AttachedFactory { get; private set; }
        public NotificationDataFactory NotificationFactory { get; private set; }

        public Task<IPlatformClientBuilder[]> GetAccountListAsync(System.Net.CookieContainer cookies)
        {
            return Task.FromResult(Enumerable.Range(0, 2)
                .Select(idx => StubGenerator.GenerateData<PlatformClientBuilder>("GetAccountListAsync", new
                    {
                        GenerateData_Id = idx,
                        AccountIndex = idx,
                    }))
                .Cast<IPlatformClientBuilder>()
                .ToArray());
        }
        public Task<bool> LoginAsync(string email, string password, IPlatformClient client)
        { return Task.Delay(3000).ContinueWith(tsk => true); }
        public Task<InitData> GetInitDataAsync(IPlatformClient client)
        { return Task.FromResult(StubGenerator.GenerateData<InitData>("GetInitDataAsync", 0)); }
        public Task<Tuple<CircleData[], ProfileData[]>> GetCircleDatasAsync(IPlatformClient client)
        {
            var profiles = Enumerable.Range(0, 3)
                .Select(idx => GenerateProfileData(idx, ProfileUpdateApiFlag.LookupCircle, "GetCircleDatasAsync"))
                .ToArray();
            var circles = StubGenerator.GenerateData<CircleData[]>("GetCircleDatasAsync",
                new object[]{
                    new{GenerateData_Id = 0, Id = StubGenerator.GenerateSetter("anyone"), Name = StubGenerator.GenerateSetter("全員"), Members = StubGenerator.GenerateSetter(new[]{profiles[0],profiles[1],profiles[2]})},
                    new{GenerateData_Id = 1, Members = StubGenerator.GenerateSetter(new[]{profiles[0],profiles[1]})},
                    new{GenerateData_Id = 2, Members = StubGenerator.GenerateSetter(new[]{profiles[1],profiles[2]})},
                    new{GenerateData_Id = 3, Members = StubGenerator.GenerateSetter(new ProfileData[]{ })},
                    new{GenerateData_Id = 4, Id = StubGenerator.GenerateSetter("15"), Name = StubGenerator.GenerateSetter("ブロック中"), Members = StubGenerator.GenerateSetter(new ProfileData[]{ GenerateProfileData(3, ProfileUpdateApiFlag.LookupCircle, "GetCircleDatasAsync") })},
                });
            return Task.FromResult(Tuple.Create(circles, profiles));
        }
        public Task<ProfileData> GetProfileLiteAsync(string profileId, IPlatformClient client)
        { return Task.FromResult(GenerateProfileData(int.Parse(profileId.Substring(2)), ProfileUpdateApiFlag.LookupProfile, "GetProfileLiteAsync")); }
        public Task<ProfileData> GetProfileFullAsync(string profileId, IPlatformClient client)
        { return Task.FromResult(GenerateProfileData(int.Parse(profileId.Substring(2)), ProfileUpdateApiFlag.ProfileGet, "GetProfileFullAsync")); } 
        public Task<ProfileData> GetProfileAboutMeAsync(IPlatformClient client)
        { return Task.FromResult(GenerateProfileData(0, ProfileUpdateApiFlag.ProfileGet, "GetProfileAboutMeAsync")); }
        public Task<ProfileData[]> GetFollowingProfilesAsync(string profileId, IPlatformClient client)
        {
            var pid = int.Parse(profileId.Substring(2));
            return Task.FromResult(Enumerable.Range(pid + 1, 3).Select(
                id => GenerateProfileData(id, ProfileUpdateApiFlag.Base, "GetFollowingProfilesAsync")).ToArray());
        }
        public Task<ProfileData[]> GetFollowedProfilesAsync(string profileId, int count, IPlatformClient client)
        {
            var pid = int.Parse(profileId.Substring(2));
            return Task.FromResult(Enumerable.Range(pid + 1, 3).Select(
                id => GenerateProfileData(id, ProfileUpdateApiFlag.Base, "GetFollowedProfilesAsync")).ToArray());
        }
        public Task<ProfileData[]> GetProfileOfPusherAsync(string plusOneId, int pushCount, IPlatformClient client)
        {
            throw new NotImplementedException();
        }
        public Task<ProfileData[]> GetFollowingMeProfilesAsync(IPlatformClient client)
        {
            return Task.FromResult(Enumerable.Range(1, 3).Select(
                id => GenerateProfileData(id, ProfileUpdateApiFlag.Base, "GetFollowingMeProfilesAsync")).ToArray());
        }
        public Task<ProfileData[]> GetIgnoredProfilesAsync(IPlatformClient client)
        {
            return Task.FromResult(Enumerable.Range(4, 3).Select(
                id => GenerateProfileData(id, ProfileUpdateApiFlag.Base, "GetIgnoredProfilesAsync")).ToArray());
        }
        public Task<ActivityData> GetActivityAsync(string activityId, IPlatformClient client)
        {
            return Task.FromResult(GenerateActivityData(
                int.Parse(activityId.Substring(2)), ActivityUpdateApiFlag.GetActivity,
                Enumerable.Range(0, 3).ToArray(), "GetActivityAsync"));
        }
        public Task<Tuple<ActivityData[], string>> GetActivitiesAsync(string circleId, string profileId, string ctValue, int length, IPlatformClient client)
        {
            var continueToken = int.Parse(ctValue ?? "0");
            return Task.FromResult(Tuple.Create(Enumerable.Range(continueToken, length)
                .Select(id => GenerateActivityData(id, ActivityUpdateApiFlag.GetActivities, Enumerable.Range(0, 3).ToArray(), "GetActivitiesAsync"))
                .ToArray(), (continueToken + length).ToString()));
        }
        public Task<Tuple<NotificationData[], DateTime, string>> GetNotificationsAsync(bool isFetchNewItemMode, int length, string continueToken, IPlatformClient client)
        {
            throw new NotImplementedException();
            //return Task.FromResult(Tuple.Create(
            //    new[] {
            //        new ContentNotificationData(
            //            GenerateActivityData(1, ActivityUpdateApiFlag.Notification, Enumerable.Range(1,3).ToArray(), "notify"),
            //            NotificationsFilter.OtherPost, new[] {
            //                new ChainingNotificationData("01", GenerateProfileData(2, ProfileUpdateApiFlag.Base, "notify"), DateTime.UtcNow),
            //                new ChainingNotificationData("02", GenerateProfileData(2, ProfileUpdateApiFlag.Base, "notify"), DateTime.UtcNow)
            //            }),
            //        new NotificationData(
            //            NotificationsFilter.OtherPost, new[] {
            //                new ChainingNotificationData("03", GenerateProfileData(2, ProfileUpdateApiFlag.Base, "notify"), DateTime.UtcNow),
            //                new ChainingNotificationData("04", GenerateProfileData(2, ProfileUpdateApiFlag.Base, "notify"), DateTime.UtcNow)
            //            }),
            //        new NotificationDataWithActivity(
            //            GenerateActivityData(1, ActivityUpdateApiFlag.Notification, Enumerable.Range(1,3).ToArray(), "notify"),
            //            NotificationsFilter.OtherPost, new[] {
            //                new ChainingNotificationData("05", GenerateProfileData(2, ProfileUpdateApiFlag.Base, "notify"), DateTime.UtcNow),
            //                new ChainingNotificationData("06", GenerateProfileData(2, ProfileUpdateApiFlag.Base, "notify"), DateTime.UtcNow)
            //            })
            //    }, DateTime.UtcNow, "continueToken_notify"));
        }
        public Task<int> GetUnreadNotificationCountAsync(IPlatformClient client)
        { return Task.FromResult(0); }
        public Task<AlbumData> GetAlbumAsync(string albumId, string profileId, IPlatformClient client)
        {
            throw new NotImplementedException();
        }
        public Task<AlbumData[]> GetAlbumsAsync(string profileId, IPlatformClient client)
        {
            throw new NotImplementedException();
        }
        public Task<ImageData> GetImageAsync(string imageId, string profileId, IPlatformClient client)
        {
            throw new NotImplementedException();
        }
        public IObservable<object> GetStreamAttacher(IPlatformClient client)
        {
            return Observable.Return(GenerateActivityData(1, ActivityUpdateApiFlag.Unloaded, new int[] { }, "getStreamAttacher"));
        }
        public Task UpdateNotificationCheckDateAsync(DateTime value, IPlatformClient client) { return Task.Factory.StartNew(() => { }); }
        public Task<ActivityData> PostActivity(string content, Dictionary<string, string> targetCircles, Dictionary<string, string> targetUsers, bool isDisabledComment, bool isDisabledReshare, IPlatformClient client)
        { return Task.Delay(500).ContinueWith(tsk => GenerateActivityData(0, ActivityUpdateApiFlag.GetActivity, new int[0], "PostComment")); }
        public Task<CommentData> PostComment(string activityId, string content, IPlatformClient client)
        { return Task.Delay(500).ContinueWith(tsk => GenerateCommentData(0, activityId,  "PostComment")); }
        public Task<CommentData> EditComment(string activityId, string commentId, string content, IPlatformClient client)
        { return Task.Delay(500).ContinueWith(tsk => GenerateCommentData(0, activityId, "EditComment")); }
        public Task DeleteComment(string commentId, IPlatformClient client)
        { return Task.Delay(500); }
        public Task MarkAsReadAsync(NotificationData target, IPlatformClient client)
        { return Task.Delay(100); }
        public Task MutateBlockUser(Tuple<string, string>[] userIdAndNames, AccountBlockType blockType, BlockActionType status, IPlatformClient client)
        { return Task.Delay(0); }

        ActivityData GenerateActivityData(int id, ActivityUpdateApiFlag flagMode, int[] commentIds, string marking)
        {
            return StubGenerator.GenerateData<ActivityData>(marking, new
            {
                GenerateData_Id = id,
                LoadedApiTypes = StubGenerator.GenerateSetter(flagMode),
                Comments = commentIds.Select(cid => new
                {
                    GenerateData_Id = cid,
                    ActivityId = StubGenerator.GenerateSetter(string.Format("Id{0:00}", id)),
                }),
            });
        }
        CommentData GenerateCommentData(int id, string activityId, string marking)
        {
            return StubGenerator.GenerateData<CommentData>(marking, new
            {
                GenerateData_Id = id,
                ActivityId = activityId
            });
        }
        ProfileData GenerateProfileData(int id, ProfileUpdateApiFlag apiType, string marking)
        {
            return StubGenerator.GenerateData<ProfileData>(marking, new
            {
                GenerateData_Id = id,
                LoadedApiTypes = StubGenerator.GenerateSetter(apiType),
            });
        }
    }
}

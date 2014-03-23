using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.Web.GooglePlus
{
    using SunokoLibrary.Threading;
    using SunokoLibrary.Web.GooglePlus;
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class DefaultAccessorStab : IApiAccessor
    {
        public Task<IPlatformClientBuilder[]> GetAccountList(System.Net.CookieContainer cookies)
        {
            return Task.Run(() =>
                new IPlatformClientBuilder[]
                {
                    new PlatformClientBuilder("builder_email", "builder_name", "builder_iconurl", 0, null, null),
                    new PlatformClientBuilder("builder_email", "builder_name", "builder_iconurl", 1, null, null),
                });
        }
        public Task<bool> LoginAsync(string email, string password, IPlatformClient client)
        { return Task.FromResult(true); }
        public Task<InitData> GetInitDataAsync(IPlatformClient client)
        {
            return Task.FromResult(new InitData(
                "atValue_hmInit", "pvtValue_hmInit", "ejxValue_hmInit",
                new CircleData[]{
                    new CircleData("cid01", "cname01_hmInit", null),
                    new CircleData("cid02", "cname02_hmInit", null)
                },
                Enumerable.Range(0, 3).Select(id => GenerateActivityData(
                    id, ActivityUpdateApiFlag.GetActivities, Enumerable.Range(0,3).ToArray(), "hmInit")).ToArray()));
        }
        public Task<Tuple<CircleData[], ProfileData[]>> GetCircleDatasAsync(IPlatformClient client)
        {
            var profiles = Enumerable.Range(1, 3)
                .Select(id => GenerateProfileData(id, ProfileUpdateApiFlag.LookupCircle, "circle"))
                .ToArray();
            return Task.FromResult(Tuple.Create(
                new CircleData[]{
                    new CircleData("anyone", "全員", new ProfileData[] { profiles[0], profiles[1], profiles[2] }),
                    new CircleData("cid02", "cname02", new ProfileData[] { profiles[0], profiles[1] }),
                    new CircleData("cid03", "cname03", new ProfileData[] { profiles[1], profiles[2] }),
                    new CircleData("cid04", "cname04", new ProfileData[] { }),
                    new CircleData("15", "ブロック中", new ProfileData[] { GenerateProfileData(3, ProfileUpdateApiFlag.LookupCircle, "circle") })
                }, profiles));
        }
        public Task<ProfileData> GetProfileLiteAsync(string profileId, IPlatformClient client)
        { return Task.FromResult(GenerateProfileData(int.Parse(profileId.Substring(3)), ProfileUpdateApiFlag.LookupProfile, "profileLite")); }
        public Task<ProfileData> GetProfileFullAsync(string profileId, IPlatformClient client)
        { return Task.FromResult(GenerateProfileData(int.Parse(profileId), ProfileUpdateApiFlag.ProfileGet, "profileFull")); } 
        public Task<ProfileData> GetProfileAboutMeAsync(IPlatformClient client)
        { return Task.FromResult(GenerateProfileData(0, ProfileUpdateApiFlag.ProfileGet, "profileMe")); }
        public Task<ProfileData[]> GetFollowingProfilesAsync(string profileId, IPlatformClient client)
        {
            var pid = int.Parse(profileId);
            return Task.FromResult(
                Enumerable.Range(pid + 1, 3).Select(id => GenerateProfileData(id, ProfileUpdateApiFlag.Base, "followingFrmoP")).ToArray());
        }
        public Task<ProfileData[]> GetFollowedProfilesAsync(string profileId, int count, IPlatformClient client)
        {
            var pid = int.Parse(profileId);
            return Task.FromResult(
                Enumerable.Range(pid + 1, 3).Select(id => GenerateProfileData(id, ProfileUpdateApiFlag.Base, "followedFrmoP")).ToArray());
        }
        public Task<ProfileData[]> GetProfileOfPusherAsync(string plusOneId, int pushCount, IPlatformClient client)
        {
            throw new NotImplementedException();
        }
        public Task<ProfileData[]> GetFollowingMeProfilesAsync(IPlatformClient client)
        {
            return Task.FromResult(
                Enumerable.Range(2, 3).Select(id => GenerateProfileData(id, ProfileUpdateApiFlag.Base, "follower")).ToArray());
        }
        public Task<ProfileData[]> GetIgnoredProfilesAsync(IPlatformClient client)
        {
            return Task.FromResult(
                Enumerable.Range(5, 3).Select(id => GenerateProfileData(id, ProfileUpdateApiFlag.Base, "ignore")).ToArray());
        }
        public Task<ActivityData> GetActivityAsync(string activityId, IPlatformClient client)
        {
            return Task.FromResult(
                GenerateActivityData(int.Parse(activityId.Substring(3)), ActivityUpdateApiFlag.GetActivity,
                Enumerable.Range(0, 3).ToArray(), "getActivity"));
        }
        public Task<Tuple<ActivityData[], string>> GetActivitiesAsync(string circleId, string profileId, string ctValue, int length, IPlatformClient client)
        {
            var continueToken = int.Parse(ctValue ?? "0");
            return Task.FromResult(Tuple.Create(
                Enumerable.Range(continueToken, length)
                    .Select(id => GenerateActivityData(id, ActivityUpdateApiFlag.GetActivities, Enumerable.Range(0, 3).ToArray(), "getActivities"))
                    .ToArray(), (continueToken + length).ToString()));
        }
        public Task<Tuple<NotificationData[], DateTime, string>> GetNotificationsAsync(bool isFetchNewItemMode, int length, string continueToken, IPlatformClient client)
        {
            return Task.FromResult(Tuple.Create(
                new[] {
                    new NotificationDataWithActivity(
                        GenerateActivityData(1, ActivityUpdateApiFlag.Notification, Enumerable.Range(1,3).ToArray(), "notify"),
                        NotificationsFilter.OtherPost, new[] {
                            new ChainingNotificationData("01", GenerateProfileData(2, ProfileUpdateApiFlag.Base, "notify"), DateTime.UtcNow),
                            new ChainingNotificationData("02", GenerateProfileData(2, ProfileUpdateApiFlag.Base, "notify"), DateTime.UtcNow)
                        }),
                    new NotificationData(
                        NotificationsFilter.OtherPost, new[] {
                            new ChainingNotificationData("03", GenerateProfileData(2, ProfileUpdateApiFlag.Base, "notify"), DateTime.UtcNow),
                            new ChainingNotificationData("04", GenerateProfileData(2, ProfileUpdateApiFlag.Base, "notify"), DateTime.UtcNow)
                        }),
                    new NotificationDataWithActivity(
                        GenerateActivityData(1, ActivityUpdateApiFlag.Notification, Enumerable.Range(1,3).ToArray(), "notify"),
                        NotificationsFilter.OtherPost, new[] {
                            new ChainingNotificationData("05", GenerateProfileData(2, ProfileUpdateApiFlag.Base, "notify"), DateTime.UtcNow),
                            new ChainingNotificationData("06", GenerateProfileData(2, ProfileUpdateApiFlag.Base, "notify"), DateTime.UtcNow)
                        })
                }, DateTime.UtcNow, "continueToken_notify"));
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
        public Task<CommentData> PostComment(string activityId, string content, IPlatformClient client)
        { return Task.FromResult(GenerateCommentData(0, 0, "postComment")); }
        public Task<CommentData> EditComment(string activityId, string commentId, string content, IPlatformClient client)
        { return Task.FromResult(GenerateCommentData(0, 0, "editComment")); }
        public Task DeleteComment(string commentId, IPlatformClient client)
        { return Task.Run(() => { }); }
        public Task MarkAsReadAsync(NotificationData target, IPlatformClient client)
        { return Task.Run(() => { }); }

        ActivityData GenerateActivityData(int id, ActivityUpdateApiFlag flagMode, int[] commentIds, string marking)
        {
            switch (flagMode)
            {
                case ActivityUpdateApiFlag.GetActivities:
                case ActivityUpdateApiFlag.GetActivity:
                case ActivityUpdateApiFlag.Notification:
                    return new ActivityData(
                        string.Format("aid{0:00}", id),
                        string.Format("ahtml{0:00}_{1}", id, marking),
                        string.Format("atext{0:00}_{1}", id, marking),
                        new StyleElement(StyleType.None, new[] { new TextElement(string.Format("aelement{0:00}_{1}", id, marking)) }),
                        false,
                        new Uri(string.Format("http://aurl.com/{0:00}_{0}", id, marking)),
                        commentIds.Select(cid => GenerateCommentData(cid, cid, marking)).ToArray(),
                        DateTime.UtcNow, DateTime.MinValue,
                        ServiceType.Desktop, PostStatusType.First, null,
                        GenerateProfileData(5, ProfileUpdateApiFlag.LookupCircle, marking), DateTime.UtcNow,
                        flagMode);
                default:
                    throw new NotImplementedException();
            }
        }
        CommentData GenerateCommentData(int commentId, int profileId, string marking)
        {
            return new CommentData(
                string.Format("cid{0:00}", commentId),
                string.Format("caid{0:00}", "aid00"),
                string.Format("chtml{0:00}_{1}", commentId, marking),
                DateTime.UtcNow, DateTime.MinValue,
                GenerateProfileData(profileId, ProfileUpdateApiFlag.Base, marking),
                PostStatusType.First);
        }
        ProfileData GenerateProfileData(int id, ProfileUpdateApiFlag apiType, string marking)
        {
            switch (apiType)
            {
                case ProfileUpdateApiFlag.Base:
                case ProfileUpdateApiFlag.LookupCircle:
                    return new ProfileData(
                        string.Format("{0:00}", id), string.Format("pname{0:00}_{1}", id, marking),
                        string.Format("http://picon/s0/{0:00}_{1}", id, marking), AccountStatus.Active, loadedApiTypes: apiType,
                        lastUpdateLookupProfile: apiType == ProfileUpdateApiFlag.LookupProfile ? new Nullable<DateTime>(DateTime.UtcNow) : null,
                        lastUpdateProfileGet: apiType == ProfileUpdateApiFlag.ProfileGet ? new Nullable<DateTime>(DateTime.UtcNow) : null);
                case ProfileUpdateApiFlag.LookupProfile:
                case ProfileUpdateApiFlag.ProfileGet:
                    return new ProfileData(
                        string.Format("{0:00}", id),
                        string.Format("pname{0:00}_{1}", id, marking),
                        string.Format("http://picon/s0/{0:00}_{1}", id, marking),
                        AccountStatus.Active,
                        string.Format("pfnam{0:00}_{1}", id, marking),
                        string.Format("plnam{0:00}_{1}", id, marking),
                        string.Format("pintr{0:00}_{1}", id, marking),
                        string.Format("pbrag{0:00}_{1}", id, marking),
                        string.Format("poccu{0:00}_{1}", id, marking),
                        string.Format("pgree{0:00}_{1}", id, marking),
                        string.Format("pnick{0:00}_{1}", id, marking),
                        RelationType.Engaged, GenderType.Other, null, null, null, null, null, null, null, null,
                        new string[] { string.Format("pplace{0:00}_{1}", id, marking), },
                        new string[] { string.Format("pother{0:00}_{1}", id, marking), }, apiType,
                        apiType == ProfileUpdateApiFlag.LookupProfile ? new Nullable<DateTime>(DateTime.UtcNow) : null,
                        apiType == ProfileUpdateApiFlag.ProfileGet ? new Nullable<DateTime>(DateTime.UtcNow) : null);
                default:
                    throw new NotImplementedException();

            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public interface IPlatformClientBuilder
    {
        string Email { get; }
        string Name { get; }
        string IconUrl { get; }
        Task<PlatformClient> Build();
    }
    public interface IPlatformClient
    {
        HttpClient NormalHttpClient { get; }
        HttpClient StreamHttpClient { get; }
        CookieContainer Cookies { get; }
        Uri PlusBaseUrl { get; }
        Uri TalkBaseUrl { get; }
        string AtValue { get; }
        string PvtValue { get; }
        string EjxValue { get; }
    }
    public interface IApiAccessor
    {
        Task<IPlatformClientBuilder[]> GetAccountList(CookieContainer cookies);
        Task<bool> LoginAsync(string email, string password, IPlatformClient client);
        Task<InitData> GetInitDataAsync(IPlatformClient client);
        Task<Tuple<CircleData[], ProfileData[]>> GetCircleDatasAsync(IPlatformClient client);
        Task<ProfileData> GetProfileLiteAsync(string profileId, IPlatformClient client);
        Task<ProfileData> GetProfileFullAsync(string profileId, IPlatformClient client);
        Task<ProfileData> GetProfileAboutMeAsync(IPlatformClient client);
        Task<ProfileData[]> GetFollowingProfilesAsync(string profileId, IPlatformClient client);
        Task<ProfileData[]> GetFollowedProfilesAsync(string profileId, int count, IPlatformClient client);
        Task<ProfileData[]> GetFollowingMeProfilesAsync(IPlatformClient client);
        Task<ProfileData[]> GetIgnoredProfilesAsync(IPlatformClient client);
        Task<ProfileData[]> GetProfileOfPusherAsync(string plusOneId, int pushCount, IPlatformClient client);
        Task<ActivityData> GetActivityAsync(string activityId, IPlatformClient client);
        Task<Tuple<ActivityData[], string>> GetActivitiesAsync(string circleId, string profileId, string ctValue, int length, IPlatformClient client);
        Task<Tuple<NotificationData[], DateTime, string>> GetNotificationsAsync(NotificationsFilter filter, int length, string continueToken, IPlatformClient client);
        Task<AlbumData> GetAlbumAsync(string albumId, string profileId, IPlatformClient client);
        Task<AlbumData[]> GetAlbumsAsync(string profileId, IPlatformClient client);
        Task<ImageData> GetImageAsync(string imageId, string profileId, IPlatformClient client);
        IObservable<object> GetStreamAttacher(IPlatformClient client);
        Task UpdateNotificationCheckDateAsync(DateTime value, IPlatformClient client);
        Task PostComment(string activityId, string content, IPlatformClient client);
        Task EditComment(string activityId, string commentId, string content, IPlatformClient client);
        Task DeleteComment(string commentId, IPlatformClient client);
    }

    /// <summary>APIが所定の目的を果たせずエラーを返した時に使用されます。</summary>
    public class ApiErrorException : Exception
    {
        public ApiErrorException(string message, ErrorType type, Uri requestUrl, HttpContent requestEntity, Exception innerException)
            : base(message, innerException)
        {
            Type = type;
            RequestUrl = requestUrl;
            RequestEntity = requestEntity;
        }
        public Uri RequestUrl { get; private set; }
        public HttpContent RequestEntity { get; private set; }
        public ErrorType Type { get; private set; }
    }
    public enum ErrorType { ParameterError, SessionError, NetworkError, UnknownError }
}

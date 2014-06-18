using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus
{
    using SunokoLibrary.Threading;
    using SunokoLibrary.Web.GooglePlus.Primitive;

    [System.Diagnostics.DebuggerDisplay("{Id,nq}, {PostStatus}")]
    public class ActivityInfo : AccessorBase
    {
        public ActivityInfo(PlatformClient client, ActivityData data)
            : base(client)
        {
            _data = data;
            _postUser = data.Owner != null ? client.People.InternalGetAndUpdateProfile(data.Owner) : null;
            _attachedContent = data.AttachedContent != null ? AttachedContentDecorator(data.AttachedContent, client) : null;
            _comments = _data.Comments != null ? _data.Comments.Select(dt => new CommentInfo(Client, dt, _data, this)).ToArray() : null;
        }
        ActivityData _data;
        ProfileInfo _postUser;
        IAttachable _attachedContent;
        CommentInfo[] _comments;

        public ActivityUpdateApiFlag LoadedApiTypes { get { return _data.LoadedApiTypes; } }
        public string Id { get { return _data.Id; } }
        public bool IsEditable { get { return CheckFlag(() => _data.IsEditable, () => PostStatus, () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない").Value; } }
        public int CommentLength { get { return CheckFlag(() => _data.CommentLength, () => PostStatus, () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない").Value; } }
        public string Html { get { return CheckFlag(() => _data.Html, () => PostStatus, () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない"); } }
        public string Text { get { return CheckFlag(() => _data.Text, () => PostStatus, () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない"); } }
        public StyleElement ParsedText { get { return CheckFlag(() => _data.ParsedText, () => PostStatus, () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない"); } }
        public Uri PostUrl { get { return CheckFlag(() => _data.PostUrl, () => PostStatus, () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない"); } }
        public DateTime PostDate { get { return CheckFlag(() => _data.PostDate, () => PostStatus, () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない").Value; } }
        public DateTime EditDate { get { return CheckFlag(() => _data.EditDate, () => PostStatus, () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない").Value; } }
        public PostStatusType PostStatus { get { return CheckFlag(() => _data.PostStatus, () => LoadedApiTypes, () => _data.LoadedApiTypes > ActivityUpdateApiFlag.Unloaded, "ActivityUpdateApiFlag.Unloadedである").Value; } }
        public ProfileInfo PostUser { get { return CheckFlag(() => _postUser, () => PostStatus, () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない"); } }
        public IAttachable AttachedContent { get { return CheckFlag(() => _attachedContent, () => PostStatus, () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない"); } }
        public ServiceType ServiceType { get { return CheckFlag(() => _data.ServiceType, () => PostStatus, () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない"); } }
        //public PlusOneInfo PlusOne { get { return CheckFlag(() => _data.AttachedContent, "PostStatus", _data.PostStatus | PostStatusType.First, PostStatusType.First); } }

        public IObservable<CommentInfo> GetComments(bool allowGetActivity, bool isInfinityEnum)
        {
            var obs = Observable
                .Defer(() => UpdateGetActivityAsync(
                    false, allowGetActivity ? ActivityUpdateApiFlag.GetActivity : ActivityUpdateApiFlag.Unloaded).ToObservable())
                .SelectMany(unit => _comments);
            if (isInfinityEnum)
                obs = obs.Concat(Client.Activity.GetStream()
                    .OfType<CommentInfo>()
                    .Where(inf => inf.ParentActivity.Id == Id)
                    .Catch<CommentInfo, ApiErrorException>(ex => Observable.Throw(new FailToOperationException<ActivityInfo>("CommentInfoの受信中にエラーが発生しました。", this, ex), (CommentInfo)null)));
            return obs;
        }
        public IObservable<ActivityInfo> GetUpdatedActivity()
        {
            return Client.Activity.GetStream()
                .OfType<ActivityInfo>().Where(info => info.Id == Id);
        }
        public Task UpdateGetActivityAsync(bool isForced, ActivityUpdateApiFlag updaterTypes)
        {
            var cache = Client.Activity.InternalGetActivityCache(_data.Id);
            return cache.SyncerUpdateActivity.LockAsync(
                isForced, () => _data.PostStatus != PostStatusType.Removed && (LoadedApiTypes & updaterTypes) != updaterTypes,
                async () =>
                {
                    try
                    {
                        var nwData = Client.Activity.InternalUpdateActivity(await Client.ServiceApi.GetActivityAsync(Id, Client));
                        if (_data.Comments != null)
                        {
                            var nwComments = from newComments in nwData.Comments
                                             join oldComments in _data.Comments on newComments.CommentId equals oldComments.CommentId into c
                                             from d in c.DefaultIfEmpty()
                                             where d == null
                                             select newComments;
                            var rmComments = from oldComments in _data.Comments
                                             join newComments in nwData.Comments on oldComments.CommentId equals newComments.CommentId into c
                                             from d in c.DefaultIfEmpty()
                                             where d == null
                                             select new CommentData(
                                                 oldComments.CommentId, oldComments.ActivityId, oldComments.Html,
                                                 oldComments.PostDate, oldComments.EditDate, oldComments.Owner,
                                                 PostStatusType.Removed);
                            foreach (var item in nwComments.Concat(rmComments))
                                Client.Activity.InternalSendObjectToStream(item);
                        }
                        else
                            foreach (var item in nwData.Comments)
                                Client.Activity.InternalSendObjectToStream(item);
                        _data = nwData;
                        _postUser = Client.People.InternalGetAndUpdateProfile(_data.Owner);
                        _attachedContent = _data.AttachedContent != null ? AttachedContentDecorator(_data.AttachedContent, Client) : null;
                        _comments = _data.Comments.Select(dt => new CommentInfo(Client, dt, _data, this)).ToArray();
                    }
                    catch (ApiErrorException e)
                    {
                        if (e.InnerException is System.Net.WebException
                            && ((System.Net.WebException)e.InnerException).Status == System.Net.WebExceptionStatus.UnknownError)
                            Client.Activity.InternalUpdateActivity(new ActivityData(Id, status: PostStatusType.Removed, updaterTypes: ActivityUpdateApiFlag.GetActivity));
                        else
                            throw new FailToOperationException<ActivityInfo>("UpdateGetActivityAsync()に失敗しました。", this, e);
                    }
                },
                () =>
                {
                    _data = cache.Value;
                    _postUser = Client.People.InternalGetAndUpdateProfile(_data.Owner);
                    _attachedContent = _data.AttachedContent != null ? AttachedContentDecorator(_data.AttachedContent, Client) : null;
                    if (_data.Comments != null)
                        _comments = _data.Comments.Select(dt => new CommentInfo(Client, dt, _data, this)).ToArray();
                });
        }
        public async Task<bool> PostComment(string content)
        {
            try
            {
                var commeData = await Client.ServiceApi.PostComment(Id, content, Client);
                Client.Activity.InternalSendObjectToStream(commeData);
                return true;
            }
            catch (ApiErrorException e)
            {
                switch (e.Type)
                {
                    case ErrorType.SessionError:
                    case ErrorType.ParameterError:
                        return false;
                    default:
                        throw new FailToOperationException<ActivityInfo>("コメント投稿に失敗しました。", this, e);
                }
            }
        }
        public async Task<bool> EditComment(string commentId, string content)
        {
            try
            {
                var commeData = await Client.ServiceApi.EditComment(Id, commentId, content, Client);
                Client.Activity.InternalSendObjectToStream(commeData);
                return true;
            }
            catch (ApiErrorException e)
            {
                switch (e.Type)
                {
                    case ErrorType.SessionError:
                    case ErrorType.ParameterError:
                        return false;
                    default:
                        throw new FailToOperationException<ActivityInfo>("コメント投稿に失敗しました。", this, e);
                }
            }
        }
        public async Task<bool> DeleteComment(string commentId)
        {
            try
            {
                await Client.ServiceApi.DeleteComment(commentId, Client);
                return true;
            }
            catch (ApiErrorException e)
            {
                switch (e.Type)
                {
                    case ErrorType.SessionError:
                    case ErrorType.ParameterError:
                        return false;
                    default:
                        throw new FailToOperationException<ActivityInfo>("コメント投稿に失敗しました。", this, e);
                }
            }
        }
        public static IAttachable AttachedContentDecorator(IAttachable info, PlatformClient client)
        {
            switch(info.Type)
            {
                case ContentType.Album:
                    return new AttachedAlbum(client, (AttachedAlbumData)info);
                case ContentType.Image:
                    return new AttachedImage(client, (AttachedImageData)info);
                case ContentType.Reshare:
                    return new AttachedPost(client, (AttachedPostData)info);
                default:
                    return info;
            }
        }
    }

    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay_TypeName,nq}")]
    public class ServiceType
    {
        public ServiceType(string id)
        {
            Id = id;
            if (id != null)
                Segments = id.Split(':');
        }
        public ServiceType(string id, params string[] segments)
        {
            Id = id;
            Segments = segments ?? new string[] { };
        }
        public readonly string Id;
        public readonly string[] Segments;
        public override bool Equals(object obj)
        {
            var tmp = obj as ServiceType;
            return obj != null && tmp != null ? Id == tmp.Id : false;
        }
        public override int GetHashCode()
        { return Id.GetHashCode(); }

        static ServiceType()
        {
            Desktop = new ServiceType("s:updates:esshare", "s", "updates", "esshare");
            Mobile = new ServiceType("s:updatesmobile:esshare", "s", "updates", "esshare");
            Checkins = new ServiceType("s:updatesmobile:checkins", "s", "updatesmobile", "checkins");
            Hangout = new ServiceType("s:talk:gcomm", "s", "talk", "gcomm");
            Unknown = new ServiceType(null);
        }
        public readonly static ServiceType Desktop;
        public readonly static ServiceType Mobile;
        public readonly static ServiceType Checkins;
        public readonly static ServiceType Hangout;
        public readonly static ServiceType Unknown;

        public static bool operator ==(ServiceType valueA, ServiceType valueB)
        { return (object)valueA != null && !valueA.Equals(valueB) || (object)valueA == null && (object)valueB == null; }
        public static bool operator !=(ServiceType valueA, ServiceType valueB)
        { return !(valueA == valueB); }

        string DebuggerDisplay_TypeName
        {
            get
            {
                string tmp;
                if (Id == ServiceType.Desktop.Id)
                    tmp = "Desktop";
                else if (Id == ServiceType.Mobile.Id)
                    tmp = "Mobile";
                else if (Id == ServiceType.Checkins.Id)
                    tmp = "Checkins";
                else if (Id == ServiceType.Hangout.Id)
                    tmp = "Hangout";
                else
                    tmp = "ServiceType.Unknown";
                return string.Format("({1}){{Id = {0}}}", Id, tmp);
            }
        }
    }
    public enum ActivityUpdateApiFlag { Unloaded, Notification, GetActivities, GetActivity }
    public enum PostStatusType { Removed, First, Edited }
}

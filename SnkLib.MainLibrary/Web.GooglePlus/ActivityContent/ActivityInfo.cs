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
        readonly Dictionary<EventHandler, IDisposable> _talkgadgetBindObjs = new Dictionary<EventHandler, IDisposable>();

        public string Id { get { return _data.Id; } }
        public ActivityUpdateApiFlag LoadedApiTypes { get { return _data.LoadedApiTypes; } }
        public bool IsEditable { get { return CheckFlag(_data.IsEditable, "PostStatus", () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない").Value; } }
        public string Html { get { return CheckFlag(_data.Html, "PostStatus", () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない"); } }
        public string Text { get { return CheckFlag(_data.Text, "PostStatus", () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない"); } }
        public StyleElement ParsedText { get { return CheckFlag(_data.ParsedText, "PostStatus", () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない"); } }
        public Uri PostUrl { get { return CheckFlag(_data.PostUrl, "PostStatus", () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない"); } }
        public DateTime PostDate { get { return CheckFlag(_data.PostDate, "PostStatus", () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない").Value; } }
        public DateTime EditDate { get { return CheckFlag(_data.EditDate, "PostStatus", () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない").Value; } }
        public PostStatusType PostStatus { get { return CheckFlag(_data.PostStatus, "LoadedApiTypes", () => _data.LoadedApiTypes > ActivityUpdateApiFlag.Unloaded, "ActivityUpdateApiFlag.Unloadedである").Value; } }
        public ProfileInfo PostUser { get { return CheckFlag(_postUser, "PostStatus", () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない"); } }
        public IAttachable AttachedContent { get { return CheckFlag(_attachedContent, "PostStatus", () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない"); } }
        public ServiceType ServiceType { get { return CheckFlag(_data.ServiceType, "PostStatus", () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない"); } }
        //public PlusOneInfo PlusOne { get { return CheckFlag(_data.AttachedContent, "PostStatus", _data.PostStatus | PostStatusType.First, PostStatusType.First); } }

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
        public Task UpdateGetActivityAsync(bool isForced, ActivityUpdateApiFlag updaterTypes, TimeSpan? intervalRestriction = null)
        {
            var cache = Client.Activity.InternalGetActivityCache(_data.Id);
            return cache.SyncerUpdateActivity.LockAsync(
                isForced, () => _data.PostStatus != PostStatusType.Removed && (LoadedApiTypes & updaterTypes) != updaterTypes,
                intervalRestriction,
                async () =>
                {
                    try
                    {
                        _data = Client.Activity.InternalUpdateActivity(await Client.ServiceApi.GetActivityAsync(Id, Client));
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
                    _comments = _data.Comments.Select(dt => new CommentInfo(Client, dt, _data, this)).ToArray();
                });
        }
        public async Task<bool> PostComment(string content)
        {
            try
            {
                await Client.ServiceApi.PostComment(Id, content, Client);
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
                await Client.ServiceApi.EditComment(Id, commentId, content, Client);
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

        public event EventHandler Refreshed
        {
            add
            {
                if (value == null)
                    return;
                lock (_talkgadgetBindObjs)
                    _talkgadgetBindObjs.Add(value, Client.Activity.GetStream()
                        .OfType<ActivityInfo>()
                        .Where(info => info.Id == Id)
                        .Subscribe(info =>
                            {
                                value(this, new EventArgs());
                                if (info != this)
                                    _data += info._data;
                            }));
            }
            remove
            {
                IDisposable obj;
                lock (_talkgadgetBindObjs)
                    if (_talkgadgetBindObjs.TryGetValue(value, out obj))
                    {
                        _talkgadgetBindObjs.Remove(value);
                        obj.Dispose();
                    }
            }
        }
    }
}

﻿using Newtonsoft.Json.Linq;
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

    [System.Diagnostics.DebuggerTypeProxy(typeof(ActivityInfoDebugView)),
    System.Diagnostics.DebuggerDisplay("{Id,nq}, {PostStatus}")]
    public class ActivityInfo : AccessorBase
    {
        public ActivityInfo(PlatformClient client, ActivityData data)
            : base(client)
        {
            _data = data;
            _postUser = data.Owner != null ? client.Relation.InternalGetAndUpdateProfile(data.Owner) : null;
        }
        ActivityData _data;
        ProfileInfo _postUser;
        CommentInfo[] _comments;
        readonly Dictionary<EventHandler, IDisposable> _talkgadgetBindObjs = new Dictionary<EventHandler,IDisposable>();

        public string Id { get { return _data.Id; } }
        public ActivityUpdateApiFlag LoadedApiTypes { get { return _data.LoadedApiTypes; } }
        public bool IsEditable { get { return CheckFlag(_data.IsEditable, "PostStatus", () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない").Value; } }
        public string Html { get { return CheckFlag(_data.Html, "PostStatus", () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない"); } }
        public string Text { get { return CheckFlag(_data.Text, "PostStatus", () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない"); } }
        public Uri PostUrl { get { return CheckFlag(_data.PostUrl, "PostStatus", () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない"); } }
        public DateTime PostDate { get { return CheckFlag(_data.PostDate, "PostStatus", () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない").Value; } }
        public DateTime EditDate { get { return CheckFlag(_data.EditDate, "PostStatus", () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない").Value; } }
        public PostStatusType PostStatus { get { return CheckFlag(_data.PostStatus, "PostStatus", () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない").Value; } }
        public ProfileInfo PostUser { get { return CheckFlag(_postUser, "PostStatus", () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない"); } }
        public IAttachable AttachedContent { get { return CheckFlag(_data.AttachedContent, "PostStatus", () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない"); } }
        public ServiceType ServiceType { get { return CheckFlag(_data.ServiceType, "PostStatus", () => _data.PostStatus >= PostStatusType.First, "PostStatusType.First以上でない"); } }
        //public PlusOneInfo PlusOne { get { return CheckFlag(_data.AttachedContent, "PostStatus", _data.PostStatus | PostStatusType.First, PostStatusType.First); } }

        public IObservable<CommentInfo> GetComments(bool isInfinityEnum)
        {
            var obs = Observable
                .If(() => (_data.GetActivityDate ?? DateTime.MinValue) < Client.Activity.BeganTimeToBind,
                    Observable.Defer(() => UpdateGetActivityAsync(false, ActivityUpdateApiFlag.GetActivity).ToObservable()),
                    Observable.Return(Unit.Default))
                .SelectMany(unit => _comments);
            if (isInfinityEnum)
                obs = obs.Concat(Client.Activity.GetStream().OfType<CommentInfo>().Where(inf => inf.ParentActivity.Id == Id));
            return obs;
        }
        public async Task UpdateGetActivityAsync(bool isForced, ActivityUpdateApiFlag updaterTypes, TimeSpan? intervalRestriction = null)
        {
            var cache = Client.Activity.InternalGetActivityCache(_data.Id);
            await cache.SyncerUpdateActivity.LockAsync(
                isForced, () => _data.PostStatus != PostStatusType.Removed && (LoadedApiTypes & updaterTypes) != updaterTypes,
                intervalRestriction,
                async () =>
                {
                    try
                    {
                        _data = Client.Activity.InternalUpdateActivity(await Client.ServiceApi.GetActivityAsync(Id, Client));
                        _postUser = Client.Relation.InternalGetAndUpdateProfile(_data.Owner);
                        _comments = _data.Comments.Select(dt => new CommentInfo(Client, dt, _data)).ToArray();
                    }
                    catch (ApiErrorException e)
                    {
                        if (e.InnerException is System.Net.WebException
                            && ((System.Net.WebException)e.InnerException).Status == System.Net.WebExceptionStatus.UnknownError)
                            Client.Activity.InternalUpdateActivity(new ActivityData(
                                Id, status: PostStatusType.Removed, updaterTypes: ActivityUpdateApiFlag.GetActivity));
                        else
                            throw new FailToOperationException("UpdateGetActivityAsync()に失敗しました。", e);
                    }
                },
                () =>
                    {
                        _data = cache.Value;
                        _postUser = Client.Relation.InternalGetAndUpdateProfile(_data.Owner);
                        _comments = _data.Comments.Select(dt => new CommentInfo(Client, dt, _data)).ToArray();
                    });
        }
        public Task<bool> PostComment(string content)
        { return Client.ServiceApi.PostComment(Id, content, Client); }
        public Task<bool> EditComment(string commentId, string content)
        { return Client.ServiceApi.EditComment(Id, commentId, content, Client); }
        public Task<bool> DeleteComment(string commentId)
        { return Client.ServiceApi.DeleteComment(commentId, Client); }
        public StyleElement GetParsedContent()
        { return ContentElement.ParseHtml(Html, Client); }

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

        class ActivityInfoDebugView
        {
            public ActivityInfoDebugView(ActivityInfo myhashtable) { _target = myhashtable; }
            static Dictionary<int, string> _profileUpdateApiFlagBitDesc =
                new Dictionary<int, string>() { { 1, "GetActivities" }, { 2, "Notification" }, { 4, "GetActivity" } };
            ActivityInfo _target;

            public string Id { get { return _target.Id; } }
            [System.Diagnostics.DebuggerDisplay("{LoadedApiTypes,nq}")]
            public string LoadedApiTypes
            {
                get
                {
                    return string.Join(", ", _profileUpdateApiFlagBitDesc
                        .Where(pair => ((int)_target.LoadedApiTypes & pair.Key) == pair.Key)
                        .Select(pair => pair.Value));
                }
            }
            public bool? IsEditable { get { return _target._data.IsEditable; } }
            public string Html { get { return _target._data.Html; } }
            public string Text { get { return _target._data.Text; } }
            public Uri PostUrl { get { return _target._data.PostUrl; } }
            public DateTime? PostDate { get { return _target._data.PostDate; } }
            public DateTime? EditDate { get { return _target._data.EditDate; } }
            public PostStatusType? PostStatus { get { return _target._data.PostStatus; } }
            public ProfileData Owner { get { return _target._data.Owner; } }
            public IAttachable AttachedContent { get { return _target._data.AttachedContent; } }
            public ServiceType SericeType { get { return _target._data.ServiceType; } }
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
        public string Id { get; private set; }
        public string[] Segments { get; private set; }
        public bool IsMobile { get { return Segments.Length > 1 ? Segments[1] == "updatesmobile" : false; } }
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
        static ServiceType _unknown;
        public static ServiceType Desktop { get; private set; }
        public static ServiceType Mobile { get; private set; }
        public static ServiceType Checkins { get; private set; }
        public static ServiceType Hangout { get; private set; }
        public static ServiceType Unknown
        {
            get
            {
                Debug_BreakIfIsAttached();
                return _unknown;
            }
            set { _unknown = value; }
        }

        public static bool operator ==(ServiceType valueA, ServiceType valueB)
        { return (object)valueA != null && !valueA.Equals(valueB) || (object)valueA == null && (object)valueB == null; }
        public static bool operator !=(ServiceType valueA, ServiceType valueB)
        { return !(valueA == valueB); }

        [System.Diagnostics.Conditional("DEBUG")]
        static void Debug_BreakIfIsAttached()
        {
            if (System.Diagnostics.Debugger.IsAttached)
                System.Diagnostics.Debugger.Break();
        }
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
                return string.Format("({1}){{Id = {0}, IsMobile = {2}}}", Id, tmp, IsMobile);
            }
        }
    }
    public enum PostStatusType { First = 1, Edited = 3, Removed = 0, }
    public enum ActivityUpdateApiFlag { Unloaded = 0, GetActivities = 1, Notification = 3, GetActivity = 7 }
}

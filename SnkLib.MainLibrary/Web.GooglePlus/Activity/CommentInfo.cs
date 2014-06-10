using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Linq;

namespace SunokoLibrary.Web.GooglePlus
{
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class CommentInfo : AccessorBase
    {
        public CommentInfo(
            PlatformClient client, CommentData commentData, ActivityData activityData)
            : base(client)
        {
            _commentData = commentData;
            _activityData = activityData;
            _owner = commentData.Owner != null ? client.People.InternalGetAndUpdateProfile(commentData.Owner) : null;
            _parentActivity = Client.Activity.InternalGetAndUpdateActivity(activityData);
            _talkgadgetBindObjs = new Dictionary<EventHandler, IDisposable>();
        }
        //ActivityInfo初期化はこちらの特殊仕様を使う。
        //これはActivityInfo用のCommentInfo、CommentInfo用のActivityInfoという無限ループ回避用処置
        internal CommentInfo(
            PlatformClient client, CommentData commentData, ActivityData activityData, ActivityInfo activityInfo)
            : base(client)
        {
            _commentData = commentData;
            _activityData = activityData;
            _owner = commentData.Owner != null ? client.People.InternalGetAndUpdateProfile(commentData.Owner) : null;
            _parentActivity = activityInfo;
            _talkgadgetBindObjs = new Dictionary<EventHandler, IDisposable>();
        }
        CommentData _commentData;
        ActivityData _activityData;
        ProfileInfo _owner;
        ActivityInfo _parentActivity;
        Dictionary<EventHandler, IDisposable> _talkgadgetBindObjs = new Dictionary<EventHandler, IDisposable>();

        public string Id { get { return _commentData.CommentId; } }
        public string Html { get { return CheckFlag(() => _commentData.Html, () => Status, () => _commentData.Status >= PostStatusType.First, "PostStatusType.First以上でない"); } }
        public DateTime PostDate { get { return CheckFlag(() => _commentData.PostDate, () => Status, () => _commentData.Status >= PostStatusType.First, "PostStatusType.First以上でない"); } }
        public DateTime EditDate { get { return CheckFlag(() => _commentData.EditDate, () => Status, () => _commentData.Status >= PostStatusType.First, "PostStatusType.First以上でない"); } }
        public ProfileInfo Owner { get { return CheckFlag(() => _owner, () => Status, () => _commentData.Status >= PostStatusType.First, "PostStatusType.First以上でない"); } }
        public ActivityInfo ParentActivity { get { return _parentActivity; } }
        public PostStatusType Status { get { return _commentData.Status; } }
        //public PlusOneInfo PlusOne { get; private set; }

        public StyleElement GetParsedContent() { return ContentElement.ParseHtml(Html, Client); }
        public async Task<bool> Edit(string content)
        {
            try
            {
                await Client.ServiceApi.EditComment(ParentActivity.Id, Id, content, Client);
                return true;
            }
            catch (ApiErrorException)
            { return false; }
        }
        public async Task<bool> Delete()
        {
            try
            {
                await Client.ServiceApi.DeleteComment(Id, Client);
                return true;
            }
            catch (ApiErrorException)
            { return false; }
        }
    }
}

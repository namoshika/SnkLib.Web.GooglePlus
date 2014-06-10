using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus
{
    using SunokoLibrary.Threading;
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class PlusOneInfo : AccessorBase
    {
        public PlusOneInfo(PlatformClient client, ActivityInfo target)
            : base(client)
        {
            _lastUpdateMember = DateTime.MinValue;
            _targetId = "buzz:" + target.Id;
            _activity = target;
        }
        public PlusOneInfo(PlatformClient client, CommentInfo target)
            : base(client)
        {
            _lastUpdateMember = DateTime.MinValue;
            _targetId = "comment:" + target.Id;
            _activity = target.ParentActivity;
        }        
        bool _isUpdatedMembers;
        string _targetId;
        ActivityInfo _activity;
        DateTime _lastUpdateMember;
        string[] _members;
        readonly System.Threading.SemaphoreSlim _syncerChange = new System.Threading.SemaphoreSlim(1, 1);
        readonly System.Threading.SemaphoreSlim _syncerGetPushMembers = new System.Threading.SemaphoreSlim(1, 1);

        public string PlusOneId { get; private set; }
        /// <summary>PlusOneボタンの状態</summary>
        public bool IsPushed { get; private set; }
        /// <summary>PlusOneボタンを押された数</summary>
        public int PushCount { get; private set; }

        /// <summary>PlusOneボタンのトグル状態を変更します。</summary>
        /// <param name="status">変更後のボタンの状態</param>
        //public async Task<bool> Push(bool status)
        //{
        //    try
        //    {
        //        await _syncerChange.WaitAsync();
        //        if (IsPushed == status)
        //            return true;

        //        var json = JToken.Parse(await ApiWrapper.ConnectToPlusOne(Client.NormalHttpClient, Client.PlusBaseUrl, _targetId, status, Client.AtValue));
        //        var plusJson = json[0][1][1];
        //        if (plusJson != null)
        //            Parse(plusJson);
        //        return true;
        //    }
        //    catch (ApiErrorException)
        //    { return false; }
        //    finally
        //    { _syncerChange.Release(); }
        //}
        /// <summary>PlusOneした人一覧を取得します。</summary>
        /// <param name="isForced">再読み込み強制の有効無効</param>
        public Task<ProfileInfo[]> GetPushMembers(bool isForced)
        { return GetPushMembers(isForced, TimeSpan.FromSeconds(1)); }
        /// <summary>PlusOneした人一覧を取得します。</summary>
        /// <param name="isForced">再読み込み強制の有効無効</param>
        /// <param name="intervalRestriction">前回の更新からの最低経過時間</param>
        public async Task<ProfileInfo[]> GetPushMembers(bool isForced, TimeSpan intervalRestriction)
        {
            try
            {
                await _syncerGetPushMembers.WaitAsync();
                if (PlusOneId == null)
                    //アップデートでParse経由にPlusOneIdが更新される
                    return new ProfileInfo[] { };
                if (_isUpdatedMembers && isForced == false
                    || isForced && DateTime.UtcNow - _lastUpdateMember < intervalRestriction)
                    return _members.Select(id => Client.People.GetProfileOf(id)).ToArray();

                var resLst = await Client.ServiceApi.GetProfileOfPusherAsync(PlusOneId, PushCount, Client);
                _members = resLst.Select(data => data.Id).ToArray();
                _lastUpdateMember = DateTime.UtcNow;
                _isUpdatedMembers = true;
                return resLst.Select(data => Client.People.InternalGetAndUpdateProfile(data)).ToArray();
            }
            finally
            { _syncerGetPushMembers.Release(); }
        }
        internal void Parse(JToken json)
        {
            PlusOneId = (string)json[0] ?? PlusOneId;
            IsPushed = (int)(long)(((JValue)json[13]).Value ?? (long)0) == 1;
            PushCount = (int)(long)(((JValue)json[16]).Value ?? (long)0);
        }
    }
}

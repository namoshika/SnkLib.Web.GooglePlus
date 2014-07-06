using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus
{
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class NotificationInfoWithActor : NotificationInfo
    {
        public NotificationInfoWithActor(SocialNotificationData data, NotificationInfoContainer container, PlatformClient client)
            : base(data, container, client)
        {
            _actionLogs = new ObservableCollection<NotificationItemInfo>(
                data.LogItems.Select(dt => new NotificationItemInfo(dt, client)));
            Actor = client.People.InternalGetAndUpdateProfile(data.LogItems.First().Actor);
            ActionLogs = new ReadOnlyObservableCollection<NotificationItemInfo>(_actionLogs);
        }
        ObservableCollection<NotificationItemInfo> _actionLogs;
        public ReadOnlyObservableCollection<NotificationItemInfo> ActionLogs { get; private set; }
        public ProfileInfo Actor { get; private set; }

        internal override void Update(NotificationData data)
        {
            base.Update(data);

            //新しい通知を追加し、キープされた通知を再配置する
            var newDatas = ((SocialNotificationData)data).LogItems;
            var oldItemInfo = _actionLogs;
            var updatedDatas = (from nwItm in newDatas
                                join oldItm in oldItemInfo on nwItm.RawData equals oldItm.RawData into nwOldPair
                                from kpdItm in nwOldPair.DefaultIfEmpty()
                                select new { NewItemData = nwItm, OldItemInfo = kpdItm }).ToArray();
            foreach (var item in updatedDatas.Select((obj, idx) => new { Index = idx, Result = obj }))
                if (item.Result.OldItemInfo == null)
                    _actionLogs.Insert(item.Index, new NotificationItemInfo(item.Result.NewItemData, Client));
                else
                {
                    var oldIdx = 0;
                    if ((oldIdx = _actionLogs.IndexOf(item.Result.OldItemInfo)) >= 0 && item.Index != oldIdx)
                        _actionLogs.Move(oldIdx, item.Index);
                }
            //古い通知を削除
            for (var i = updatedDatas.Length; i < _actionLogs.Count; i++)
                _actionLogs.RemoveAt(i);

            if (Actor.Id != _actionLogs.First().Actor.Id)
                Actor = Client.People.InternalGetAndUpdateProfile(((SocialNotificationData)data).Actor);
        }
    }
    public class NotificationItemInfo : AccessorBase
    {
        public NotificationItemInfo(NotificationItemData data, PlatformClient client)
            : base(client)
        {
            _data = data;
            Actor = Client.People.InternalGetAndUpdateProfile(_data.Actor);
        }
        NotificationItemData _data;

        public string RawData { get { return _data.RawData; } }
        public NotificationFlag Type { get { return _data.Type; } }
        public ProfileInfo Actor { get; private set; }
    }
}

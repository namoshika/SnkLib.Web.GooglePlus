using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus
{
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public class NotificationInfoContainer : AccessorBase
    {
        public NotificationInfoContainer(PlatformClient client, bool isReadedItemOnly)
            : base(client)
        {
            _isReadedItemOnly = isReadedItemOnly;
            _notifications = new ObservableCollection<NotificationInfo>();
            Notifications = new ReadOnlyObservableCollection<NotificationInfo>(_notifications);
        }
        ObservableCollection<NotificationInfo> _notifications;
        bool _isReadedItemOnly;
        string _continueToken;

        public ReadOnlyObservableCollection<NotificationInfo> Notifications { get; private set; }
        public Task UpdateAsync(int length) { return PrivateUpdate(length, null); }
        public Task LoadMore(int length) { return PrivateUpdate(length, _continueToken); }
        async Task PrivateUpdate(int length, string continueToken)
        {
            try
            {
                var apiResult = await Client.ServiceApi.GetNotificationsAsync(_isReadedItemOnly, length, continueToken, Client);
                var idx = 0;
                if (continueToken == null)
                {
                    foreach (var item in apiResult.Item1
                        .GroupJoin(
                            _notifications,
                            newItem => newItem.Id,
                            oldItem => oldItem.Id,
                            (nw, old) => new { NewItem = nw, OldItem = old.DefaultIfEmpty() })
                        .SelectMany(
                            pair => pair.OldItem, (pair, oldItem) => new { NewData = pair.NewItem, OldInfo = oldItem }))
                    {
                        if (item.OldInfo != null)
                        {
                            var oldIdx = _notifications.IndexOf(item.OldInfo);
                            _notifications.Move(oldIdx, idx);
                            item.OldInfo.Update(item.NewData);
                        }
                        else
                            if (item.NewData is StreamNotificationData)
                                _notifications.Insert(idx, new NotificationInfoWithActivity((StreamNotificationData)item.NewData, this, Client));
                            else if (item.NewData is CircleNotificationData)
                                _notifications.Insert(idx, new NotificationInfoWithActor((CircleNotificationData)item.NewData, this, Client));
                            else
                                _notifications.Insert(idx, new NotificationInfo(item.NewData, this, Client));
                        idx++;
                    }
                    for (; idx < _notifications.Count;)
                        _notifications.RemoveAt(idx);
                }
                else
                {
                    foreach (var item in apiResult.Item1)
                    {
                        if (item is StreamNotificationData)
                            _notifications.Add(new NotificationInfoWithActivity((StreamNotificationData)item, this, Client));
                        else if (item is CircleNotificationData)
                            _notifications.Add(new NotificationInfoWithActor((CircleNotificationData)item, this, Client));
                        else
                            _notifications.Add(new NotificationInfo(item, this, Client));
                    }
                }
                _continueToken = apiResult.Item3;
            }
            catch (ApiErrorException e)
            { throw new FailToOperationException<NotificationInfoContainer>("NotificationInfoContainerの更新に失敗しました。", this, e); }
        }
    }
}

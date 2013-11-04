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
        public NotificationInfoContainer(PlatformClient client, NotificationsFilter filtter = NotificationsFilter.All)
            : base(client)
        {
            _notifications = new ObservableCollection<NotificationInfo>();
            _filtter = filtter;
            Notifications = new ReadOnlyObservableCollection<NotificationInfo>(_notifications);
        }
        NotificationsFilter _filtter;
        ObservableCollection<NotificationInfo> _notifications;
        string _continueToken;

        public int UnreadItemCount { get; set; }
        public DateTime LastReadedTime { get; private set; }
        public ReadOnlyObservableCollection<NotificationInfo> Notifications { get; private set; }

        public Task UpdateAsync(int length) { return PrivateUpdate(length, null); }
        public Task LoadMore(int length) { return PrivateUpdate(length, _continueToken); }
        public Task UpdateLatestCheckTimeAsync(DateTime? lastReadTime = null)
        { return Client.ServiceApi.UpdateNotificationCheckDateAsync(lastReadTime ?? DateTime.UtcNow, Client); }
        async Task PrivateUpdate(int length, string continueToken)
        {
            var apiResult = await Client.ServiceApi.GetNotificationsAsync(_filtter, length, continueToken, Client);
            var unreadItemCount = 0;
            LastReadedTime = apiResult.Item2;
            if (continueToken == null)
            {
                foreach (var item in apiResult.Item1
                    .GroupJoin(
                        _notifications,
                        newItem => newItem.FollowingNotifications.Last().Id,
                        oldItem => oldItem.FollowingNotifications.Last().Id,
                        (nw, old) => new { NewItem = nw, OldItem = old.DefaultIfEmpty() })
                    .Reverse()
                    .SelectMany(
                        pair => pair.OldItem, (pair, oldItem) => new { NewData = pair.NewItem, OldInfo = oldItem }))
                {
                    if (LastReadedTime < item.NewData.NoticedDate)
                        unreadItemCount++;
                    if (item.OldInfo != null)
                    {
                        var oldIdx = _notifications.IndexOf(item.OldInfo);
                        _notifications.Move(oldIdx, 0);
                        item.OldInfo.Update(item.NewData);
                    }
                    else
                        if (item.NewData is NotificationDataWithActivity)
                            _notifications.Insert(0, new NotificationInfoWithActivity((NotificationDataWithActivity)item.NewData, this, Client));
                        else if (item.NewData is NotificationDataWithImage)
                            _notifications.Insert(0, new NotificationInfoWithImage((NotificationDataWithImage)item.NewData, this, Client));
                        else
                            _notifications.Insert(0, new NotificationInfo(item.NewData, this, Client));
                }
                for (var i = length; length < _notifications.Count; i++)
                    _notifications.RemoveAt(length);
                UnreadItemCount = unreadItemCount;
            }
            else
            {
                foreach (var item in apiResult.Item1)
                {
                    if (LastReadedTime < item.NoticedDate)
                        unreadItemCount++;
                    if (item is NotificationDataWithActivity)
                        _notifications.Add(new NotificationInfoWithActivity((NotificationDataWithActivity)item, this, Client));
                    else if (item is NotificationDataWithImage)
                        _notifications.Add(new NotificationInfoWithImage((NotificationDataWithImage)item, this, Client));
                    else
                        _notifications.Add(new NotificationInfo(item, this, Client));
                }
                UnreadItemCount += unreadItemCount;
            }
            _continueToken = apiResult.Item3;
        }
    }
}

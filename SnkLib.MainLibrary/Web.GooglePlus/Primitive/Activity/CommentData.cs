using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class CommentData : CoreData
    {
        public CommentData(string id, string activityId, string html, DateTime commentDate, DateTime editDate,
            ProfileData owner, PostStatusType status)
        {
            CommentId = id;
            ActivityId = activityId;
            Html = html;
            PostDate = commentDate;
            EditDate = editDate;
            Status = status;
            Owner = owner;
        }
        public readonly PostStatusType Status;
        [Identification]
        public readonly string CommentId;
        [Identification]
        public readonly string ActivityId;
        public readonly string Html;
        public readonly DateTime PostDate;
        public readonly DateTime EditDate;
        public readonly ProfileData Owner;
        //public PlusOneInfo PlusOne;

        public static CommentData operator +(CommentData value1, CommentData value2)
        {
            return new CommentData(
                Merge(value1, value2, obj => obj.CommentId),
                Merge(value1, value2, obj => obj.ActivityId),
                Merge(value1, value2, obj => obj.Html),
                Merge(value1, value2, obj => obj.PostDate),
                Merge(value1, value2, obj => obj.EditDate),
                Merge(value1, value2, obj => obj.Owner, true),
                Merge(value1, value2, obj => obj.Status));
        }
    }
}

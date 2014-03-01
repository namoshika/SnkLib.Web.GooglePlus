using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class CommentData : DataBase
    {
        public CommentData(
            string commentId, string activityId, string html, DateTime commentDate, DateTime editDate, ProfileData owner, PostStatusType status)
        {
            CommentId = commentId;
            ActivityId = activityId;
            Html = html;
            PostDate = commentDate;
            EditDate = editDate;
            Status = status;
            Owner = owner;
        }
        public PostStatusType Status { get; protected set; }
        public string CommentId { get; protected set; }
        public string ActivityId { get; protected set; }
        public string Html { get; private set; }
        public DateTime PostDate { get; private set; }
        public DateTime EditDate { get; private set; }
        public ProfileData Owner { get; private set; }
        //public PlusOneInfo PlusOne { get; private set; }

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

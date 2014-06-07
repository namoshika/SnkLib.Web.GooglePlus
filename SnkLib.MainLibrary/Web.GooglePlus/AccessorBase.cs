using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus
{
    using SunokoLibrary.Threading;

    public abstract class AccessorBase
    {
        public AccessorBase(PlatformClient client) { Client = client; }
        protected PlatformClient Client { get; private set; }

        public static Tuple<PlatformClient, T[]>[] GroupByClient<T>(IEnumerable<T> enumerable)
        {
            var groups = new Dictionary<PlatformClient, List<AccessorBase>>();
            var other = new List<T>();
            foreach (var item in enumerable)
                if (item is AccessorBase)
                {
                    var client = (item as AccessorBase).Client;
                    List<AccessorBase> list;
                    if (groups.TryGetValue(client, out list) == false)
                        groups.Add(client, list = new List<AccessorBase>());
                    list.Add(item as AccessorBase);
                }
                else
                    other.Add(item);
            return groups
                .Select(pair => Tuple.Create(pair.Key, pair.Value.Cast<T>().ToArray()))
                .Concat(new[] { Tuple.Create((PlatformClient)null, other.ToArray()) })
                .ToArray();
        }
        public static TResult CheckFlag<TResult>(Func<TResult> targetGetter, Expression<Func<object>> targetNameGetter, Func<bool> checkProc, string errorMessage)
        {
            if (checkProc() == false)
            {
                string memberName;
                switch (targetNameGetter.Body.NodeType)
                {
                    case ExpressionType.Convert:
                        memberName = ((MemberExpression)((UnaryExpression)targetNameGetter.Body).Operand).Member.Name;
                        break;
                    case ExpressionType.MemberAccess:
                        memberName = ((MemberExpression)targetNameGetter.Body).Member.Name;
                        break;
                    default:
                        throw new Exception();
                }
                throw new InvalidOperationException(
                    string.Format("{0}プロパティが{1}状態で各プロパティを参照する事はできません。",
                    memberName, errorMessage));
            }
            return targetGetter();
        }
    }
    public class FailToOperationException : Exception
    {
        public FailToOperationException(string message, Exception innerException)
            : base(message, innerException) { }
    }
    public class FailToOperationException<T> : FailToOperationException
    {
        public FailToOperationException(string message, T errorTarget, Exception innerException)
            : base(message, innerException) { Info = errorTarget; }
        public T Info { get; private set; }
    }
}

using SunokoLibrary.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus
{
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
        public static TResult CheckFlag<TResult>(TResult target, string checkTargetName, Func<bool> checkProc, string errorMessage)
        {
            if (checkProc() == false)
                throw new InvalidOperationException(
                    string.Format("{0}プロパティが{1}状態で各プロパティを参照する事はできません。", checkTargetName, errorMessage));
            return target;
        }
    }
}

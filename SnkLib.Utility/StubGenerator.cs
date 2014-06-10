using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Utility
{
    using SunokoLibrary.Web.GooglePlus.Primitive;

    public interface IValueGenerator
    {
        bool IsGeneratable(Type targetType);
        object Generate(Type targetType, string name, string marking, dynamic constructConfig);
    }
    public abstract class StubGenerator : IValueGenerator
    {
        public StubGenerator(params Type[] generatingTypes) { _generatableTypes = generatingTypes; }
        readonly Type[] _generatableTypes;
        public bool IsGeneratable(Type targetType)
        { return _generatableTypes.Any(typ => IsSubOrEqualClass(targetType, typ)); }
        public abstract object Generate(Type targetType, string name, string marking, dynamic constructConfig);

        static readonly IDataFactoryManager DefaultManager = new DataFactoryManager();
        static readonly IValueGenerator[] Generators = new IValueGenerator[] {
            new StringUriGenerator(), new ArrayGenerator(),
            new ObjectGenerator<ProfileData>(DefaultManager.ProfileFactory),
            new ObjectGenerator<ActivityData>(DefaultManager.ActivityFactory),
            new ObjectGenerator<CommentData>(DefaultManager.CommentFactory),
            new ObjectGenerator<object>(null),
        };
        public static IValueGenerator GenerateSetter<T>(T value) { return new ObjectGenerator<T>(value); }
        public static TResult GenerateData<TResult>(string marking, dynamic parameter)
        { return (TResult)GenerateData(typeof(TResult), null, marking, parameter); }
        protected static object GenerateData(Type targetType, string name, string marking, dynamic constructConfig)
        {
            if (constructConfig is IValueGenerator && ((IValueGenerator)constructConfig).IsGeneratable(targetType) == false)
                throw new ArgumentException(string.Format("{0}型の値生成に失敗。指定された引数constructConfigで指定されたIValueGeneratorは{0}の値の生成に対応していません。", targetType.Name));

            constructConfig = constructConfig is int ? new { GenerateData_Id = constructConfig } : constructConfig;
            var generator = constructConfig is IValueGenerator ? (IValueGenerator)constructConfig : Generators.FirstOrDefault(gen => gen.IsGeneratable(targetType));
            return generator.Generate(targetType, name, marking, constructConfig);
        }
        protected static bool IsSubOrEqualClass(Type self, Type checkTarget)
        { return self == checkTarget || self.IsSubclassOf(checkTarget); }
    }
    class ObjectGenerator<T> : StubGenerator
    {
        public ObjectGenerator(T value) : base(typeof(T)) { _result = value; }
        public ObjectGenerator(DataFactory<T> factory) : base(typeof(T)) { _factory = factory; }
        DataFactory<T> _factory;
        T _result;
        public override object Generate(Type targetType, string name, string marking, dynamic constructConfig)
        {
            if (_result != null)
                return _result;
            else if (Attribute.IsDefined(targetType, typeof(StubableAttribute)) || _factory != null)
            {
                var id = (int)constructConfig.GenerateData_Id;
                var configType = (Type)constructConfig.GetType();
                var obj = _factory != null
                    ? _factory.GenerateAsStub(marking)
                    : System.Runtime.Serialization.FormatterServices.GetUninitializedObject(targetType);
                foreach (var fieldInf in targetType.GetFields(
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.GetField))
                {

                    //constructConfigに該当するメンバがある場合は、メンバの値生成用のconstructConfigとして用いる
                    var fConfInf = configType.GetProperty(fieldInf.Name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    var fConf = (fConfInf != null ? (dynamic)fConfInf.GetValue(constructConfig) : null) ?? id;

                    //未初期化のメンバのみに処理を行う。
                    //よって、既に初期化されているメンバには手を出さない
                    if (fConf is IValueGenerator == false && fieldInf.GetValue(obj) != null)
                        continue;

                    //Id属性がついているものにはmarkerを付けない
                    fieldInf.SetValue(obj, StubGenerator.GenerateData(
                        fieldInf.FieldType, fieldInf.Name,
                        Attribute.IsDefined(fieldInf, typeof(IdentificationAttribute)) ? null : marking, fConf));
                }
                return obj;
            }
            else
                return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;
        }
    }
    class StringUriGenerator : StubGenerator
    {
        public StringUriGenerator() : base(typeof(string), typeof(Uri)) { }
        public override object Generate(Type targetType, string name, string marking, dynamic constructConfig)
        {
            if (IsSubOrEqualClass(targetType, typeof(string)))
                return string.Format("{0}{1:00}", name ?? targetType.Name, constructConfig.GenerateData_Id)
                    + (string.IsNullOrEmpty(marking) ? string.Empty : string.Format("_{0}", marking));
            else if (IsSubOrEqualClass(targetType, typeof(Uri)))
                return new Uri(string.Format("http://{0}.com/{1:00}", name ?? targetType.Name, constructConfig.GenerateData_Id)
                    + (string.IsNullOrEmpty(marking) ? string.Empty : string.Format("_{0}", marking)));
            else
                throw new NotImplementedException();
        }
    }
    class ArrayGenerator : StubGenerator
    {
        public ArrayGenerator() : base(typeof(Array)) { }
        public override object Generate(Type targetType, string name, string marking, dynamic constructConfig)
        {
            //id配列か匿名型配列をconstructConfigとして受け付ける
            //指定されていない場合は既定値として長さ5で生成
            var elementType = targetType.GetElementType();
            var configArray =
                constructConfig is int[] ? ((int[])constructConfig).Select(id => new { GenerateData_Id = id }).ToArray() :
                constructConfig is IEnumerable<object> ? ((System.Collections.IEnumerable)constructConfig).Cast<object>().ToArray()
                : Enumerable.Range(0, 5).Select(id => new { GenerateData_Id = id }).ToArray();

            //取り敢えず複数生成し、nullにならなければ戻り値resultに代入する
            var resultList = Enumerable.Range(0, configArray.Length)
                .Select(idx => StubGenerator.GenerateData(elementType, null, marking, configArray[idx]))
                .TakeWhile(obj => obj != null).ToList();
            var result = Array.CreateInstance(elementType, resultList.Count);
            for (var i = 0; i < resultList.Count; i++)
                result.SetValue(resultList[i], i);
            return result;
        }
    }

    class DataFactoryManager : IDataFactoryManager
    {
        public DataFactoryManager()
        {
            ProfileFactory = new ProfileDataFactory(this);
            ActivityFactory = new ActivityDataFactory(this);
            CommentFactory = new CommentDataFactory(this);
            AttachedFactory = new AttachedDataFactory(this);
        }
        public ProfileDataFactory ProfileFactory { get; private set; }
        public ActivityDataFactory ActivityFactory { get; private set; }
        public CommentDataFactory CommentFactory { get; private set; }
        public AttachedDataFactory AttachedFactory { get; private set; }
        public NotificationDataFactory NotificationFactory { get; private set; }
    }
    static class DataFactoryEx
    {
        public static T GenerateAsStub<T>(this DataFactory<T> self, string marker)
        {
            var obj = (T)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(T));
            var objType = obj.GetType();
            var initValues = new Dictionary<Expression<Func<T, object>>, object>();
            self.GetStubModeConfig(initValues, marker);

            //objのメンバを適当な値で初期化
            //書式: { dt => dt.MemberName, Value }[]
            foreach(var pair in initValues)
            {
                string memberName;
                switch(pair.Key.Body.NodeType)
                {
                    case ExpressionType.Convert:
                        memberName = ((MemberExpression)((UnaryExpression)pair.Key.Body).Operand).Member.Name;
                        break;
                    case ExpressionType.MemberAccess:
                        memberName = ((MemberExpression)pair.Key.Body).Member.Name;
                        break;
                    default:
                        throw new Exception();
                }
                objType.GetField(memberName).SetValue(obj, pair.Value);
            }
            return obj;
        }
    }
}

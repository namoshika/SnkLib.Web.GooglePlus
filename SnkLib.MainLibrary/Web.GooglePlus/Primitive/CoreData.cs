using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public abstract class CoreData
    {
        protected static TResult Merge<T, TResult>(T baseData, T data, Func<T, TResult> accessor, bool enableAdd = false)
        {
            var val_baseData = baseData is ValueType == false && (object)baseData == null ? default(TResult) : accessor(baseData);
            var val_data = data is ValueType == false && (object)data == null ? default(TResult) : accessor(data);

            if(enableAdd)
            {
                var adderTResult = enableAdd ? GenerateCalcFunc<TResult, TResult>((pA, pB) => Expression.Add(pA, pB)) : null;
                return adderTResult(val_baseData, val_data);
            }
            else
            {
                var val = val_data is ValueType && default(TResult).Equals(val_data) == false ? val_data
                    : val_data is ValueType == false && (object)val_data != null ? val_data
                    : val_baseData;
                return val;
            }
        }
        static Func<T, T, TResult> GenerateCalcFunc<T, TResult>(Func<ParameterExpression,ParameterExpression, BinaryExpression> op)
        {
            var paramA = Expression.Parameter(typeof(T), "value1");
            var paramB = Expression.Parameter(typeof(T), "value2");
            var func = (Func<T, T, TResult>)Expression.Lambda(op(paramA, paramB), paramA, paramB).Compile();
            return func;
        }
    }
    public class StubableAttribute : Attribute { }
    public class IdentificationAttribute : Attribute { }
}

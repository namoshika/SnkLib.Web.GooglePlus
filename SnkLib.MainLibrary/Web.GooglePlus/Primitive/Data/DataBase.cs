using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SunokoLibrary.Web.GooglePlus.Primitive
{
    public class DataBase
    {
        protected static TResult Merge<T, TResult>(T baseData, T data, Func<T, TResult> accessor, bool enableAdd = false, bool precedeBaseData = false)
        {
            var notEqualerT = GenerateCalcFunc<T, bool>((pA, pB) => Expression.NotEqual(pA, pB));
            var notEqualerTResult = GenerateCalcFunc<TResult, bool>((pA, pB) => Expression.NotEqual(pA, pB));
            var adderTResult = enableAdd ? GenerateCalcFunc<TResult, TResult>((pA, pB) => Expression.Add(pA, pB)): null;

            var val_baseData = notEqualerT(baseData, default(T)) ? accessor(baseData) : default(TResult);
            var val_data = notEqualerT(data, default(T)) ? accessor(data) : default(TResult);

            return enableAdd
                ? adderTResult(val_baseData, val_data)
                : notEqualerTResult(val_data, default(TResult)) && precedeBaseData == false ? val_data : val_baseData;
        }
        static Func<T, T, TResult> GenerateCalcFunc<T, TResult>(Func<ParameterExpression,ParameterExpression, BinaryExpression> op)
        {
            var paramA = Expression.Parameter(typeof(T), "value1");
            var paramB = Expression.Parameter(typeof(T), "value2");
            var func = (Func<T, T, TResult>)Expression.Lambda(op(paramA, paramB), paramA, paramB).Compile();
            return func;
        }
    }
}

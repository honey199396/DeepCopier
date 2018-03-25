﻿using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DeepCopy
{
    /// <summary>
    /// 利用表达式树实现深拷贝的类
    /// </summary>
    public static class DeepCopyUtil
    {
        private static Type _typeUtil;

        private static Type _typeString;

        private static Type _typeIEnumerable;

        // 缓存委托
        private static ConcurrentDictionary<(Type, Type), MulticastDelegate> _caches;

        private static MethodInfo _copyMethodInfo;

        private static MethodInfo _copyArrayMethodInfo;

        private static MethodInfo _copyListMethodInfo;

        private static string _listTypeFullName;
        static DeepCopyUtil()
        {
            _typeUtil = typeof(DeepCopyUtil);
            _typeString = typeof(string);
            _typeIEnumerable = typeof(IEnumerable);
            _caches = new ConcurrentDictionary<(Type, Type), MulticastDelegate>();
            _copyMethodInfo = _typeUtil.GetMethod(nameof(Copy));
            _copyArrayMethodInfo = _typeUtil.GetMethod(nameof(CopyArray), BindingFlags.NonPublic | BindingFlags.Static);
            _copyListMethodInfo = _typeUtil.GetMethod(nameof(CopyList), BindingFlags.NonPublic | BindingFlags.Static);
            _listTypeFullName = typeof(List<>).FullName.TrimEnd('1');
        }

        /// <summary>
        /// 新建目标类型实例，并将源对象的属性值拷贝至目标对象的对应属性
        /// </summary>
        /// <typeparam name="TSource">源对象类型</typeparam>
        /// <typeparam name="TTarget">目标对象类型</typeparam>
        /// <param name="source">源对象实例</param>
        /// <returns>深拷贝了源对象属性的目标对象实例</returns>
        public static TTarget Copy<TSource, TTarget>(TSource source)
        {
            Type sourceType = typeof(TSource);
            Type targetType = typeof(TTarget);
            var key = (sourceType, targetType);
            if (_caches.TryGetValue(key, out var copy))
            {
                // 如果之前缓存过，则直接调用缓存的委托
                return (TTarget)copy.DynamicInvoke(source);
            }
            else
            {
                var parameterExpression = Expression.Parameter(sourceType, nameof(source));

                var memberBindings = new List<MemberBinding>();
                // 遍历目标对象的所有属性信息
                foreach (var targetPropInfo in targetType.GetProperties())
                {
                    // 从源对象获取同名的属性信息
                    var sourcePropInfo = sourceType.GetProperty(targetPropInfo.Name);

                    Type sourcePropType = sourcePropInfo?.PropertyType;
                    Type targetPropType = targetPropInfo.PropertyType;

                    // 只在满足以下三个条件的情况下进行拷贝
                    // 1.源属性类型和目标属性类型一致
                    // 2.源属性可读
                    // 3.目标属性可写
                    if (sourcePropType == targetPropType
                        && sourcePropInfo.CanRead
                        && targetPropInfo.CanWrite)
                    {
                        // 获取属性值的表达式
                        Expression expression = Expression.Property(parameterExpression, sourcePropInfo);

                        // 如果对象属性可以遍历（目前只支持数组和List）
                        if (_typeIEnumerable.IsAssignableFrom(targetPropType) && targetPropType != _typeString)
                        {
                            // 获取实现的IEnumerable的泛型参数
                            Type elementType;
                            MethodInfo methodInfo = null;
                            if (targetPropType.IsArray)
                            {
                                elementType = targetPropType.GetElementType();
                                // 获取添加了类型参数信息的泛型方法信息
                                methodInfo = _copyArrayMethodInfo.MakeGenericMethod(elementType);
                            }
                            else if (targetPropType.FullName.StartsWith(_listTypeFullName))
                            {
                                elementType = targetPropType.GetGenericArguments()[0];

                                methodInfo = _copyListMethodInfo.MakeGenericMethod(elementType);
                            }
                            if (methodInfo != null)
                            {
                                // 进行递归
                                expression = Expression.Call(null, methodInfo, expression);
                                memberBindings.Add(Expression.Bind(targetPropInfo, expression));
                            }
                        }
                        else
                        {
                            // 如果目标属性是值类型或者字符串，则直接做赋值处理
                            // 暂不考虑目标值类型有非字符串的引用类型这种特殊情况
                            // 非字符串引用类型做递归处理
                            if (IsRefTypeExceptString(targetPropType))
                            {
                                // 获取添加了类型参数信息的泛型方法信息
                                var method = _copyMethodInfo.MakeGenericMethod(sourcePropType, targetPropType);
                                // 进行递归
                                expression = Expression.Call(null, method, expression);
                            }
                            memberBindings.Add(Expression.Bind(targetPropInfo, expression));
                        }
                    }
                }

                var memberInitExpression = Expression
                    .MemberInit(Expression.New(targetType), memberBindings);


                var lambdaExpression = Expression.Lambda<Func<TSource, TTarget>>(
                    memberInitExpression, parameterExpression);

                copy = lambdaExpression.Compile();
                _caches.TryAdd(key, copy);
                return (TTarget)copy.DynamicInvoke(source);
            }
        }

        private static List<TElement> CopyList<TElement>(List<TElement> list)
        {
            if (list == null)
            {
                return null;
            }

            List<TElement> result = new List<TElement>();

            if (IsRefTypeExceptString(typeof(TElement)))
            {
                foreach (TElement item in list)
                {
                    result.Add(Copy<TElement, TElement>(item));
                }
            }
            else
            {
                foreach (TElement item in list)
                {
                    result.Add(item);
                }
            }
            return result;
        }

        private static TElement[] CopyArray<TElement>(TElement[] array)
        {
            if (array == null)
            {
                return null;
            }
            TElement[] result = new TElement[array.Length];
            if (IsRefTypeExceptString(typeof(TElement)))
            {
                for (int i = 0; i < array.Length; i++)
                {
                    result[i] = Copy<TElement, TElement>(array[i]);
                }
            }
            else
            {
                for (int i = 0; i < array.Length; i++)
                {
                    result[i] = array[i];
                }
            }
            return result;
        }

        /// <summary>
        /// 判断是否是string以外的引用类型
        /// </summary>
        /// <returns></returns>
        private static bool IsRefTypeExceptString(Type type)
            => !type.IsValueType && type != _typeString;
    }
}

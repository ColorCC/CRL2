﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace CRL
{
    public class ObjectConvert
    {
        /// <summary>
        /// 转化值,并处理默认值
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static object SetNullValue(object value, Type type = null)
        {
            if (type == null && value == null)
            {
                return DBNull.Value;
                //throw new Exception("至少一项不能为空");
            }
            if (value != null)
            {
                type = value.GetType();
            }
            if (type == typeof(Enum))
            {
                value = (int)value;
            }
            else if (type == typeof(DateTime))
            {
                DateTime time = (DateTime)value;
                if (time.Year == 1)
                {
                    value = DateTime.Now;
                }
            }
            else if (type == typeof(byte[]))
            {
                if (value == null)
                    return 0;
            }
            else if (type == typeof(Guid))
            {
                if (value == null)
                    return Guid.NewGuid().ToString();
            }
            else if (type == typeof(string))
            {
                value = value + "";
            }
            return value;
        }
        /// <summary>
        /// 转换为为强类型
        /// </summary>
        /// <param name="type"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal static object ConvertObject(Type type, object obj)
        {
            #region 类型转换
            if (type == typeof(Int32))
            {
                obj = Convert.ToInt32(obj);
            }
            else if (type == typeof(Int16))
            {
                obj = Convert.ToInt16(obj);
            }
            else if (type == typeof(Int64))
            {
                obj = Convert.ToInt64(obj);
            }
            else if (type == typeof(DateTime))
            {
                obj = Convert.ToDateTime(obj);
            }
            else if (type == typeof(Decimal))
            {
                obj = Convert.ToDecimal(obj);
            }
            else if (type == typeof(Double))
            {
                obj = Convert.ToDouble(obj);
            }
            else if (type == typeof(System.Byte[]))
            {
                obj = (byte[])obj;
            }
            else if (type.BaseType == typeof(System.Enum))
            {
                obj = Convert.ToInt32(obj);
            }
            else if (type == typeof(System.Boolean))
            {
                obj = Convert.ToBoolean(obj);
            }
            else if (type == typeof(Guid))
            {
                obj = new Guid(obj.ToString());
            }
            #endregion
            return obj;
        }
        /// <summary>
        /// 转换为为强类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        internal static T ConvertObject<T>(object obj)
        {
            if (obj == null)
                return default(T);
            if (obj is DBNull)
                return default(T);
            var type = typeof(T);
            return (T)ConvertObject(type, obj);
        }
        /// <summary>
        /// 把复杂对象转换为简单对象
        /// </summary>
        /// <typeparam name="TDest"></typeparam>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static TDest CloneToSimple<TDest, TSource>(TSource source)
            where TDest : class,new()
            where TSource : class,new()
        {
            var simpleTypes = TypeCache.GetProperties(typeof(TDest), false);
            var complexTypes = TypeCache.GetProperties(typeof(TSource), false);
            TDest obj = new TDest();
            foreach (Attribute.FieldAttribute info in simpleTypes.Values)
            {
                if (complexTypes.ContainsKey(info.Name))
                {
                    var complexInfo = complexTypes[info.Name];
                    object value = complexInfo.GetValue(source);

                    info.SetValue(obj, value);
                }
            }
            return obj;
        }
        /// <summary>
        /// 把复杂对象转换为简单对象
        /// 不会转换不对应的字段
        /// </summary>
        /// <typeparam name="TDest">目的</typeparam>
        /// <typeparam name="TSource">源</typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static List<TDest> CloneToSimple<TDest, TSource>(List<TSource> source)
            where TDest : class,new()
            where TSource : class,new()
        {
            var simpleTypes = TypeCache.GetProperties(typeof(TDest), false);
            var complexTypes = TypeCache.GetProperties(typeof(TSource), false);
            List<TDest> list = new List<TDest>();
            foreach (TSource item in source)
            {
                TDest obj = new TDest();
                foreach (Attribute.FieldAttribute info in simpleTypes.Values)
                {
                    if (complexTypes.ContainsKey(info.Name))
                    {
                        var complexInfo = complexTypes[info.Name];
                        object value = complexInfo.GetValue(item);

                        info.SetValue(obj, value);
                    }
                }
                list.Add(obj);
            }
            return list;
        }
        internal static List<TItem> DataReaderToList<TItem>(DbDataReader reader, bool setConstraintObj = false, ParameCollection fieldMapping = null) where TItem : class, new()
        {
            var mainType = typeof(TItem);
            return DataReaderToList<TItem>(reader,mainType, setConstraintObj, fieldMapping);
        }
        internal static List<TItem> DataReaderToList<TItem>(DbDataReader reader, Type mainType, bool setConstraintObj = false, ParameCollection fieldMapping = null) where TItem : class, new()
        {
            var list = new List<TItem>();
            var typeArry = TypeCache.GetProperties(mainType, !setConstraintObj).Values;
            while (reader.Read())
            {
                var detailItem = DataReaderToObj(reader, mainType, typeArry, fieldMapping) as TItem;
                list.Add(detailItem);
            }
            reader.Close();
            return list;
        }
        internal static object DataReaderToObj(DbDataReader reader, Type mainType, IEnumerable<Attribute.FieldAttribute> typeArry, ParameCollection fieldMapping = null)
        {
            object detailItem = System.Activator.CreateInstance(mainType);
            var columns = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                columns.Add(reader.GetName(i).ToLower());
            }
            IModel obj2 = null;
            if (detailItem is IModel)
            {
                obj2 = detailItem as IModel;
                obj2.BoundChange = false;
                var b = obj2.BoundChange;
                if (fieldMapping != null)//由lambdaQuery创建
                {
                    foreach (var name in fieldMapping.Keys)
                    {
                        obj2[name] = reader[fieldMapping[name].ToString()];
                    }
                }
            }
            foreach (Attribute.FieldAttribute info in typeArry)
            {
                if (info.FieldType == Attribute.FieldType.关联字段)//按外部字段
                {
                    string tab = TypeCache.GetTableName(info.ConstraintType);
                    string fieldName = info.GetTableFieldFormat(tab, info.ConstraintResultField);
                    var value = reader[fieldName];
                    info.SetValue(detailItem, value);
                    if (obj2 != null)
                    {
                        obj2[info.Name] = value;
                    }
                }
                else if (info.FieldType == Attribute.FieldType.关联对象)//按动态实例
                {
                    Type type = info.PropertyType;
                    object oleObject = System.Activator.CreateInstance(type);
                    string tableName = TypeCache.GetTableName(type);
                    var typeArry2 = TypeCache.GetProperties(type, true).Values;
                    foreach (Attribute.FieldAttribute info2 in typeArry2)
                    {
                        string fieldName = info2.AliasesName;
                        object value = reader[fieldName];
                        info2.SetValue(oleObject, value);
                        if (obj2 != null)
                        {
                            obj2[info2.Name] = value;
                        }
                    }
                    info.SetValue(detailItem, oleObject);
                }
                else
                {
                    if (!columns.Contains(info.Name.ToLower()))
                    {
                        continue;
                    }
                    object value = reader[info.Name];
                    info.SetValue(detailItem, value);
                }
            }
            if (obj2 != null)
            {
                obj2.BoundChange = true;
            }
            return detailItem;
        }

        

        /// <summary>
        /// DataRead转为字典
        /// </summary>
        /// <typeparam name="Tkey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        internal static Dictionary<Tkey, TValue> DataReadToDictionary<Tkey, TValue>(DbDataReader reader)
        {
            var dic = new Dictionary<Tkey, TValue>();
            while (reader.Read())
            {
                object data1 = reader[0];
                object data2 = reader[1];
                Tkey key = ConvertObject<Tkey>(data1);
                TValue value = ConvertObject<TValue>(data2);
                dic.Add(key, value);
            }
            reader.Close();
            return dic;
        }
    }
}

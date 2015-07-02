﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Data;
using System.Text.RegularExpressions;

namespace CRL
{
    /// <summary>
    /// 基本方法
    /// </summary>
    public class Base
    {
        /// <summary>
        /// 对集合进行分页
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="index">从1开始</param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public static List<T> CutList<T>(IEnumerable<T> list, int index, int pageSize) where T : class, new()
        {
            return list.Skip((index - 1) * pageSize).Take(pageSize).ToList();
        }

        /// <summary>
        /// 获取查询字段,并自动转换虚拟字段
        /// </summary>
        /// <param name="typeArry"></param>
        /// <param name="aliases">是否别名</param>
        /// <returns></returns>
        internal static string GetQueryFields(IEnumerable<Attribute.FieldAttribute> typeArry, bool aliases)
        {
            string str = "";
            foreach (Attribute.FieldAttribute info in typeArry)
            {
                if (info.FieldType == Attribute.FieldType.虚拟字段)
                {
                    str += string.Format("{0} as {1},", info.VirtualField, info.KeyWordName);
                }
                else if (string.IsNullOrEmpty(info.QueryFullName))
                {
                    str += string.Format("{0},", info.KeyWordName); 
                }
                else
                {
                    str += string.Format("{0},", info.QueryFullName);
                }
            }
            if (str.Length > 1)
            {
                str = str.Substring(0, str.Length - 1);
            }
            return str;
        }
       
        /// <summary>
        /// 检测所有对象
        /// </summary>
        /// <param name="dbHelper"></param>
        /// <param name="baseType"></param>
        /// <returns></returns>
        public static string CheckAllModel(CoreHelper.DBHelper dbHelper,Type baseType)
        {
            string msg = "";
            var helper = new CRL.DBExtend(dbHelper);
            var assembyle = System.Reflection.Assembly.GetAssembly(baseType);
            Type[] types = assembyle.GetTypes();
            List<Type> findTypes = new List<Type>();
            var typeCRL = typeof(CRL.IModel);
            foreach (var type in types)
            {
                if (type.IsClass)
                {
                    var type1 = type.BaseType;
                    while (type1.BaseType != null)
                    {
                        if (type1.BaseType == typeCRL || type1 == typeCRL)
                        {
                            findTypes.Add(type);
                            break;
                        }
                        type1 = type1.BaseType;
                    }
                }
            }

            try
            {
                foreach (var type in findTypes)
                {
                    try
                    {
                        object obj = System.Activator.CreateInstance(type);
                        CRL.IModel b = obj as CRL.IModel;
                        msg += b.CreateTable(helper);
                    }
                    catch { }
                }
            }
            catch (Exception ero) { }
            CoreHelper.EventLog.Log(msg);
            return msg;
        }

        /// <summary>
        /// SQL语句转换为存储过程
        /// </summary>
        /// <param name="template"></param>
        /// <param name="dbHelper"></param>
        /// <param name="sql"></param>
        /// <param name="procedureName"></param>
        /// <param name="templateParame"></param>
        /// <returns></returns>
        internal static string SqlToProcedure(string template, CoreHelper.DBHelper dbHelper, string sql, string procedureName, Dictionary<string, string> templateParame = null)
        {
            var adpater = DBAdapter.DBAdapterBase.GetDBAdapterBase(dbHelper.CurrentDBType);
            template = template.Trim();
            Regex r = new Regex(@"\@(\w+)", RegexOptions.IgnoreCase);//like @parame
            Match m;
            List<string> pars = new List<string>();
            for (m = r.Match(sql); m.Success; m = m.NextMatch())
            {
                string par = m.Groups[1].ToString();
                if (!pars.Contains(par))
                {
                    pars.Add(par);
                }
            }
            var typeMappint = adpater.GetFieldMapping();
            //string template = Properties.Resources.pageTemplate.Trim();
            sql = sql.Replace("'", "''");//单引号过滤
            template = template.Replace("{name}", procedureName);
            template = template.Replace("{sql}", sql);
            string parames = "";
            //构造参数
            foreach (var p in dbHelper.Params)
            {
                string key = p.Key.Replace("@","");
                var t = p.Value.GetType();
                if (!typeMappint.ContainsKey(t))
                {
                    throw new Exception(string.Format("找不到对应的字段类型映射 {0} 在 {1}", t, adpater));
                }
                var par = typeMappint[t];
                if (t == typeof(System.String))
                {
                    par = string.Format(par, 500);
                }
                parames += adpater.SpParameFormat(key, par, false);
            }
            foreach (var p in dbHelper.OutParams)
            {
                string key = p.Key;
                var t = p.Value.GetType();
                var par = typeMappint[t];
                parames += adpater.SpParameFormat(key, par, true);
            }
            if (parames.Length > 0)
            {
                parames = parames.Substring(0, parames.Length - 1);
            }
            template = template.Replace("{parame}", parames);
            if (templateParame != null)
            {
                foreach (var item in templateParame)
                {
                    var value = item.Value;
                    value = value.Replace("'", "''");//单引号过滤
                    template = template.Replace("{" + item.Key + "}", value);
                }
            }
            
            template = adpater.GetCreateSpScript(procedureName, template);
            return template;
        }
        public static string GetVersion()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly().GetName();
            return assembly.Version.ToString();
        }
    }
}

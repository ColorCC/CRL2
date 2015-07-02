﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CRL.DBAdapter
{
    internal class MySQLDBAdapter : DBAdapterBase
    {        
        #region 创建结构
        /// <summary>
        /// 创建存储过程脚本
        /// </summary>
        /// <param name="spName"></param>
        /// <returns></returns>
        public override string GetCreateSpScript(string spName, string script)
        {
            throw new NotSupportedException("MySql不支持动态创建存储过程");
            string template = string.Format(@"
drop procedure if exists {0};
EXECUTE  ' {1} ';
", spName, script);
            return template;
        }

        /// <summary>
        /// 获取字段类型映射
        /// </summary>
        /// <returns></returns>
        public override Dictionary<Type, string> GetFieldMapping()
        {
            Dictionary<Type, string> dic = new Dictionary<Type, string>();
            //字段类型对应
            dic.Add(typeof(System.String), "varchar({0})");
            dic.Add(typeof(System.Decimal), "decimal(18, 2)");
            dic.Add(typeof(System.Double), "float");
            dic.Add(typeof(System.Single), "real");
            dic.Add(typeof(System.Boolean), "tinyint(1)");
            dic.Add(typeof(System.Int32), "int");
            dic.Add(typeof(System.Int16), "SMALLINT");
            dic.Add(typeof(System.Enum), "int");
            dic.Add(typeof(System.Byte), "SMALLINT");
            dic.Add(typeof(System.DateTime), "datetime");
            dic.Add(typeof(System.UInt16), "SMALLINT");
            dic.Add(typeof(System.Object), "varchar(30)");
            dic.Add(typeof(System.Byte[]), "varbinary({0})");
            dic.Add(typeof(System.Guid), "varchar(50)");
            return dic;
        }
        /// <summary>
        /// 获取列类型和默认值
        /// </summary>
        /// <param name="info"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public override string GetColumnType(Attribute.FieldAttribute info, out string defaultValue)
        {
            Type propertyType = info.PropertyType;
            Dictionary<Type, string> dic = GetFieldMapping();
            defaultValue = info.DefaultValue;

            //int默认值
            if (string.IsNullOrEmpty(defaultValue))
            {
                if (!info.IsPrimaryKey && propertyType == typeof(System.Int32))
                {
                    defaultValue = "0";
                }
                //datetime默认值
                if (propertyType == typeof(System.DateTime))
                {
                    defaultValue = " CURRENT_TIMESTAMP";
                }
            }
            string columnType;
            if (propertyType.FullName.IndexOf("System.") > -1)
            {
                columnType = dic[propertyType];
            }
            else
            {
                propertyType = info.PropertyType.BaseType;
                columnType = dic[propertyType];
            }
            //超过3000设为ntext
            if (propertyType == typeof(System.String) && info.Length > 3000)
            {
                columnType = "varchar(8000)";
            }
            if (info.Length > 0)
            {
                columnType = string.Format(columnType, info.Length);
            }
            if (info.IsPrimaryKey)
            {
                columnType = "int primary key not  null  auto_increment";
            }
            if (!string.IsNullOrEmpty(info.ColumnType))
            {
                columnType = info.ColumnType;
            }
            return columnType;
        }

        /// <summary>
        /// 创建字段脚本
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public override string GetCreateColumnScript(Attribute.FieldAttribute field)
        {
            string str = string.Format("alter table `{0}` add {1} {2}", field.TableName, field.KeyWordName, field.ColumnType);
            if (!string.IsNullOrEmpty(field.DefaultValue))
            {
                str += string.Format(" default '{0}' ", field.DefaultValue);
            }
            if (field.NotNull)
            {
                str += " not null";
            }
            return str;
        }

        /// <summary>
        /// 创建索引脚本
        /// </summary>
        /// <param name="filed"></param>
        /// <returns></returns>
        public override string GetColumnIndexScript(Attribute.FieldAttribute filed)
        {
            string indexScript = string.Format("ALTER TABLE `{0}` ADD {2} ({1}) ", filed.TableName, filed.KeyWordName, filed.FieldIndexType == Attribute.FieldIndexType.非聚集唯一 ? "UNIQUE" : "INDEX index_name");
            return indexScript;
        }

        /// <summary>
        /// 创建表脚本
        /// </summary>
        /// <param name="fields"></param>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public override void CreateTable(DBExtend helper, List<Attribute.FieldAttribute> fields, string tableName)
        {
            var defaultValues = new List<string>();
            string script = string.Format("create table {0}(\r\n", tableName);
            List<string> list2 = new List<string>();
            foreach (Attribute.FieldAttribute item in fields)
            {
                string nullStr = item.NotNull ? "NOT NULL" : "";
                string str = string.Format("{0} {1} {2} ", item.KeyWordName, item.ColumnType, nullStr);
                if (item.IsPrimaryKey)
                {
                    str = " " + item.Name + " int primary key auto_increment";
                }
                list2.Add(str);
                
            }
            script += string.Join(",\r\n", list2.ToArray());
            script += ") ";
            helper.Execute(script);
            foreach (string s in defaultValues)
            {
                if (!string.IsNullOrEmpty(s))
                {
                    helper.Execute(s);
                }
            }
        }
        #endregion
        public override CoreHelper.DBType DBType
        {
            get { return CoreHelper.DBType.MYSQL; }
        }
        #region SQL查询
        /// <summary>
        /// 批量插入,mysql不支持批量插入
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="helper"></param>
        /// <param name="details"></param>
        /// <param name="keepIdentity"></param>
        public override void BatchInsert<TItem>(CoreHelper.DBHelper helper, List<TItem> details, bool keepIdentity = false)
        {
            foreach(var item in details)
            {
                helper.ClearParams();
                InsertObject(item, helper);
            }
            
        }

        /// <summary>
        /// 获取插入语法
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="helper"></param>
        /// <returns></returns>
        public override int InsertObject(IModel obj, CoreHelper.DBHelper helper)
        {
            Type type = obj.GetType();
            string table = TypeCache.GetTableName(type);
            var typeArry = TypeCache.GetProperties(type, true).Values;
            string sql = string.Format("insert into `{0}`(", table);
            string sql1 = "";
            string sql2 = "";
            foreach (Attribute.FieldAttribute info in typeArry)
            {
                string name = info.Name;
                if (info.IsPrimaryKey)
                    continue;
                if (!string.IsNullOrEmpty(info.VirtualField))
                {
                    continue;
                }
                object value = info.GetValue(obj);
                value = ObjectConvert.SetNullValue(value, info.PropertyType);
                sql1 += string.Format("{0},", info.KeyWordName);
                sql2 += string.Format("?{0},", name);
                helper.AddParam(name, value);
            }
            sql1 = sql1.Substring(0, sql1.Length - 1);
            sql2 = sql2.Substring(0, sql2.Length - 1);
            sql += sql1 + ") values( " + sql2 + ") ; SELECT LAST_INSERT_ID();";
            sql = SqlFormat(sql);
            return Convert.ToInt32(helper.ExecScalar(sql));
        }
        /// <summary>
        /// 获取 with(nolock)
        /// </summary>
        /// <returns></returns>
        public override string GetWithNolockFormat()
        {
            return "";
        }
        /// <summary>
        /// 获取前几条语句
        /// </summary>
        /// <param name="fields">id,name</param>
        /// <param name="query">from table where 1=1</param>
        /// <param name="top"></param>
        /// <returns></returns>
        public override string GetSelectTop(string fields, string query,string sort, int top)
        {
            string sql = string.Format("select {1} {2} {3} {0}", top == 0 ? "" : " LIMIT 0, " + top, fields, query, sort);
            return sql;
        }
        #endregion

        #region 系统查询
        public override string GetAllTablesSql(CoreHelper.DBHelper helper)
        {
            return "select lower(table_name),1 from information_schema.tables where table_schema='" + helper.DatabaseName + "' ";
        }
        public override string GetAllSPSql(CoreHelper.DBHelper helper)
        {
            return "select `name`,1 from mysql.proc where db = '" + helper.DatabaseName + "' and `type` = 'PROCEDURE' ";
        }
        #endregion

        #region 模版
        public override string SpParameFormat(string name, string type, bool output)
        {
            string str = "";
            if (!output)
            {
                str = "in {0} {1},";
            }
            else
            {
                str = "out {0} {1},";
            }
            return string.Format(str, name, type);
        }

        public override string KeyWordFormat(string value)
        {
            return string.Format("`{0}`", value);
        }
        public override string TemplateGroupPage
        {
            get
            {
                throw new NotSupportedException("MySql不支持动态创建存储过程");
                string str = @"
CREATE PROCEDURE {name}
( 
	{parame}
) 

BEGIN
 if pageSize<=1 then 
  set pageSize=20;
 end if;
 if pageIndex < 1 then 
  set pageIndex = 1; 
 end if;
 
 set @strsql = concat('select {fields} from {sql} order by {sort} limit ',_pageIndex*_pageSize-_pageSize,',',_pageSize); 
 prepare stmtsql from @strsql; 
 execute stmtsql; 
 deallocate prepare stmtsql;
 set @strsqlcount='select count(1) as count from {sql}';
 prepare stmtsqlcount from @strsqlcount; 
 execute stmtsqlcount; 
 deallocate prepare stmtsqlcount; 
END
";
                return str;
            }
        }

        public override string TemplatePage
        {
            get
            {
                throw new NotSupportedException("MySql不支持动态创建存储过程");
                string str = @"
CREATE PROCEDURE {name}
( 
	{parame}
) 

BEGIN
 if pageSize<=1 then 
  set pageSize=20;
 end if;
 if pageIndex < 1 then 
  set pageIndex = 1; 
 end if;
 
 set @strsql = concat('select {fields} from {sql} order by {sort} limit ',_pageIndex*_pageSize-_pageSize,',',_pageSize); 
 prepare stmtsql from @strsql; 
 execute stmtsql; 
 deallocate prepare stmtsql;
 set @strsqlcount='select count(1) as count from {sql}';
 prepare stmtsqlcount from @strsqlcount; 
 execute stmtsqlcount; 
 deallocate prepare stmtsqlcount; 
END

";
                return str;
            }
        }

        public override string TemplateSp
        {
            get
            {
                throw new NotSupportedException("MySql不支持动态创建存储过程");
                string str = @"
CREATE PROCEDURE {name}
({parame})
begin
	{sql};
end
";
                return str;
            }
        }
        public override string SqlFormat(string sql)
        {
            return System.Text.RegularExpressions.Regex.Replace(sql, @"@(\w+)", "?$1");
        }
        #endregion

        public override string SubstringFormat(string field, int index, int length)
        {
            throw new NotImplementedException();
        }

        public override string StringLikeFormat(string field, string parName)
        {
            throw new NotImplementedException();
        }

        public override string StringNotLikeFormat(string field, string parName)
        {
            throw new NotImplementedException();
        }

        public override string StringContainsFormat(string field, string parName)
        {
            throw new NotImplementedException();
        }

        public override string BetweenFormat(string field, string parName, string parName2)
        {
            throw new NotImplementedException();
        }

        public override string DateDiffFormat(string field, string format, string parName)
        {
            throw new NotImplementedException();
        }

        public override string InFormat(string field, string parName)
        {
            throw new NotImplementedException();
        }

        public override string NotInFormat(string field, string parName)
        {
            throw new NotImplementedException();
        }
    }
}

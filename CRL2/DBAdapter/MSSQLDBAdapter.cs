﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace CRL.DBAdapter
{
    internal class MSSQL2000DBAdapter : MSSQLDBAdapter
    {
        public override CoreHelper.DBType DBType
        {
            get { return CoreHelper.DBType.MSSQL2000; }
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
            string script = string.Format("create table [{0}] (\r\n", tableName);
            List<string> list2 = new List<string>();
            foreach (Attribute.FieldAttribute item in fields)
            {
                string nullStr = item.NotNull ? "NOT NULL" : "";
                string str = string.Format("[{0}] {1} {2} ", item.Name, item.ColumnType, nullStr);
                list2.Add(str);
                //生成默认值语句
                if (!string.IsNullOrEmpty(item.DefaultValue))
                {
                    string v = string.Format("ALTER TABLE [dbo].[{0}] ADD  CONSTRAINT [DF_{0}_{1}]  DEFAULT ({2}) FOR [{1}]", tableName, item.Name, item.DefaultValue);
                    defaultValues.Add(v);
                }
            }
            script += string.Join(",\r\n", list2.ToArray());
//            script += string.Format(@" CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED 
//(
//	[Id] ASC
//)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
//", tableName);
            script += ") ON [PRIMARY]";
            //var list3 = GetIndexScript();
            //defaultValues.AddRange(list3);
            helper.Execute(script);
            foreach (string s in defaultValues)
            {
                if (!string.IsNullOrEmpty(s))
                {
                    helper.Execute(s);
                }
            }
        }
    }
    internal class MSSQLDBAdapter : DBAdapterBase
    {
        #region 创建结构

        /// <summary>
        /// 创建存储过程脚本
        /// </summary>
        /// <param name="spName"></param>
        /// <returns></returns>
        public override string GetCreateSpScript(string spName, string script)
        {
            string template = string.Format(@"
if not exists(select * from sysobjects where name='{0}' and type='P')
begin
exec sp_executesql N' {1} '
end", spName, script);
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
            dic.Add(typeof(System.String), "nvarchar({0})");
            dic.Add(typeof(System.Decimal), "decimal(18, 2)");
            dic.Add(typeof(System.Double), "float");
            dic.Add(typeof(System.Single), "real");
            dic.Add(typeof(System.Boolean), "bit");
            dic.Add(typeof(System.Int32), "int");
            dic.Add(typeof(System.Int16), "SMALLINT");
            dic.Add(typeof(System.Enum), "int");
            dic.Add(typeof(System.Byte), "SMALLINT");
            dic.Add(typeof(System.DateTime), "datetime");
            dic.Add(typeof(System.UInt16), "SMALLINT");
            dic.Add(typeof(System.Int64), "bigint");
            dic.Add(typeof(System.Object), "nvarchar(30)");
            dic.Add(typeof(System.Byte[]), "varbinary({0})");
            dic.Add(typeof(System.Guid), "nvarchar(50)");
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
                    defaultValue = "(0)";
                }
                //datetime默认值
                if (propertyType == typeof(System.DateTime))
                {
                    defaultValue = "(getdate())";
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
                columnType = "ntext";
            }
            if (info.Length > 0)
            {
                columnType = string.Format(columnType, info.Length);
            }
            if (info.IsPrimaryKey)
            {
                columnType = "int IDENTITY(1,1) NOT NULL";
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
            string str = string.Format("alter table [{0}] add {1} {2}", field.TableName, field.KeyWordName, field.ColumnType);
            if (!string.IsNullOrEmpty(field.DefaultValue))
            {
                str += string.Format(" default({0})", field.DefaultValue);
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
            string indexScript = string.Format("CREATE {2} NONCLUSTERED INDEX  IX_INDEX_{0}_{1}  ON dbo.[{0}]([{1}])", filed.TableName, filed.Name, filed.FieldIndexType == Attribute.FieldIndexType.非聚集唯一 ? "UNIQUE" : "");
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
            string script = string.Format("create table [{0}] (\r\n", tableName);
            List<string> list2 = new List<string>();
            string primaryKey = "id";
            foreach (Attribute.FieldAttribute item in fields)
            {
                if (item.IsPrimaryKey)
                {
                    primaryKey = item.Name;
                }
                string nullStr = item.NotNull ? "NOT NULL" : "";
                string str = string.Format("{0} {1} {2} ", item.KeyWordName, item.ColumnType, nullStr);
                list2.Add(str);
                //生成默认值语句
                if (!string.IsNullOrEmpty(item.DefaultValue))
                {
                    string v = string.Format("ALTER TABLE [dbo].[{0}] ADD  CONSTRAINT [DF_{0}_{1}]  DEFAULT ({2}) FOR [{1}]", tableName, item.Name, item.DefaultValue);
                    defaultValues.Add(v);
                }
            }
            script += string.Join(",\r\n", list2.ToArray());
            script += string.Format(@" CONSTRAINT [PK_{0}] PRIMARY KEY CLUSTERED 
(
	[{1}] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]
", tableName, primaryKey);
            script += ") ON [PRIMARY]";
            //var list3 = GetIndexScript();
            //defaultValues.AddRange(list3);
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
            get { return CoreHelper.DBType.MSSQL; }
        }
        #region SQL查询

        /// <summary>
        /// 批量插入
        /// </summary>
        /// <typeparam name="TItem"></typeparam>
        /// <param name="helper"></param>
        /// <param name="details"></param>
        /// <param name="keepIdentity"></param>
        public override void BatchInsert<TItem>(CoreHelper.DBHelper helper, List<TItem> details, bool keepIdentity = false) 
        {
            string table = TypeCache.GetTableName(typeof(TItem));
            string sql = GetSelectTop("*", " from " + table + " where 1=0","", 1);
            DataTable tempTable = helper.ExecDataTable(sql);
            var typeArry = TypeCache.GetProperties(typeof(TItem), true).Values;
            foreach (TItem item in details)
            {
                DataRow dr = tempTable.NewRow();
                foreach (Attribute.FieldAttribute info in typeArry)
                {
                    string name = info.Name;
                    object value = info.GetValue(item);
                    if (!keepIdentity)
                    {
                        if (info.IsPrimaryKey)
                            continue;
                    }
                    if (!string.IsNullOrEmpty(info.VirtualField))
                    {
                        continue;
                    }
                    var value2 = ObjectConvert.SetNullValue(value,info.PropertyType);
                    dr[name] = value2;
                }
                tempTable.Rows.Add(dr);
            }
            helper.InsertFromDataTable(tempTable, table, keepIdentity);
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
            string sql = string.Format("insert into [{0}](", table);
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
                sql2 += string.Format("@{0},", name);
                helper.AddParam(name, value);
            }
            sql1 = sql1.Substring(0, sql1.Length - 1);
            sql2 = sql2.Substring(0, sql2.Length - 1);
            sql += sql1 + ") values( " + sql2 + ") ; SELECT scope_identity() ;";
            sql = SqlFormat(sql);
            return Convert.ToInt32(helper.ExecScalar(sql));
        }
        /// <summary>
        /// 获取 with(nolock)
        /// </summary>
        /// <returns></returns>
        public override string GetWithNolockFormat()
        {
            return " with(nolock)";
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
            string sql = string.Format("select {0} {1} {2} {3}", top == 0 ? "" : "top " + top, fields, query, sort);
            return sql;
        }
        #endregion

        #region 系统查询
        public override string GetAllTablesSql(CoreHelper.DBHelper helper)
        {
            return "select Lower(name),id from sysobjects where  type='u'";
        }
        public override string GetAllSPSql(CoreHelper.DBHelper helper)
        {
            return "select name,id from sysobjects where  type='P'";
        }
        #endregion

        #region 模版
        public override string SpParameFormat(string name, string type, bool output)
        {
            string str = "";
            if (!output)
            {
                str = "@{0} {1},";
            }
            else
            {
                str = "@{0} {1} output,";
            }
            return string.Format(str, name, type);
        }
        public override string KeyWordFormat(string value)
        {
            return string.Format("[{0}]", value);
        }

        public override string TemplateGroupPage
        {
            get
            {
                string str = @"
--group分页
CREATE PROCEDURE [dbo].{name}
( 
	{parame}
) 
--参数传入 @pageSize,@pageIndex
AS
set  nocount  on
declare @start nvarchar(20) 
declare @end nvarchar(20)
declare @pageCount INT

begin

    --获取记录数
	  select @count=count(0) from (select count(*) as a from  {sql}) t
    if @count = 0
        set @count = 1

    --取得分页总数
    set @pageCount=(@count+@pageSize-1)/@pageSize

    /**当前页大于总页数 取最后一页**/
    if @pageIndex>@pageCount
        set @pageIndex=@pageCount

	--计算开始结束的行号
	set @start = @pageSize*(@pageIndex-1)+1
	set @end = @start+@pageSize-1 
	SELECT * FROM (select {fields},ROW_NUMBER() OVER ( Order by {rowOver} ) AS RowNumber From {sql}) T WHERE T.RowNumber BETWEEN @start AND @end --order by {sort}
end
";
                return str;
            }
        }

        public override string TemplatePage
        {
            get
            {
                string str = @"
--表分页
CREATE PROCEDURE [dbo].{name}
( 
	{parame}
) 
--参数传入 @pageSize,@pageIndex
AS
set  nocount  on
declare @start nvarchar(20) 
declare @end nvarchar(20)
declare @pageCount INT

begin

    --获取记录数
	  select @count=count(0) from {sql}
    if @count = 0
        set @count = 1

    --取得分页总数
    set @pageCount=(@count+@pageSize-1)/@pageSize

    /**当前页大于总页数 取最后一页**/
    if @pageIndex>@pageCount
        set @pageIndex=@pageCount

	--计算开始结束的行号
	set @start = @pageSize*(@pageIndex-1)+1
	set @end = @start+@pageSize-1 
	SELECT * FROM (select {fields},ROW_NUMBER() OVER ( Order by {rowOver} ) AS RowNumber From {sql}) T WHERE T.RowNumber BETWEEN @start AND @end order by {sort}
end

";
                return str;
            }
        }

        public override string TemplateSp
        {
            get
            {
                string str = @"
CREATE PROCEDURE [dbo].{name}
({parame})
AS
set  nocount  on
	{sql}
";
                return str;
            }
        }
        public override string SqlFormat(string sql)
        {
            return sql;
        }
        #endregion

        #region 函数格式化
        public override string SubstringFormat(string field, int index, int length)
        {
            return string.Format(" SUBSTRING({0},{1},{2})", field, index, length);
        }

        public override string StringLikeFormat(string field, string parName)
        {
            return string.Format("{0} LIKE {1}", field, parName);
        }

        public override string StringNotLikeFormat(string field, string parName)
        {
            return string.Format("{0} NOT LIKE {1}", field, parName);
        }

        public override string StringContainsFormat(string field, string parName)
        {
            return string.Format("CHARINDEX({1},{0})>0", field, parName);
        }

        public override string BetweenFormat(string field, string parName, string parName2)
        {
            return string.Format("{0} between {1} and {2}", field, parName, parName2);
        }

        public override string DateDiffFormat(string field, string format, string parName)
        {
            return string.Format("DateDiff({0},{1},{2})", format, field, parName);
        }

        public override string InFormat(string field, string parName)
        {
            return string.Format("{0} IN ({1})", field, parName);
        }
        public override string NotInFormat(string field, string parName)
        {
            return string.Format("{0} NOT IN ({1})", field, parName);
        }
        #endregion
    }
}

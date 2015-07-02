﻿using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace FormTest.Code
{
    public class LocalSqlHelper
    {
        static CoreHelper.DBHelper CreateDbHelper(string name)
        {
            string connString;
            //mssql
            //更改DBConnection目录内数据连接文件
            connString = CoreHelper.CustomSetting.GetConnectionString(name);
            return new CoreHelper.SqlHelper(connString);

            ////mysql
            //connString = "Server=127.0.0.1;Port=3306;Stmt=;Database=testDB; User=root;Password=;";
            //return new CoreHelper.MySqlHelper(connString);

            //oracle
            //connString = "Data Source={0};User ID={1};Password={2};Integrated Security=no;";
            //connString = string.Format(connString, "orcl", "SCOTT", "a123");
            //return new CoreHelper.OracleHelper(connString);
        }
        public static CoreHelper.DBHelper TestConnection
        {
            get
            {
                return CreateDbHelper("Default");
            }
        }
    }
}

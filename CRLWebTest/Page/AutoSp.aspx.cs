﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using CRL;
namespace WebTest
{
    public partial class AutoSp : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }
        //对应的数据库帐号需要有创建存储过程的权限
        //以下查询会成成类似 ZautoSp_1E719B3EE5AFF6F4 的储储过程,删除后再查询时会自动生成
        protected void Button1_Click(object sender, EventArgs e)
        {
            var query = Code.ProductDataManage.Instance.GetLamadaQuery();
            //会按条件不同创建不同的存储过程
            query = query.Where(b => b.Id < 700);
            string name = Request["name"];
            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(b => b.InterFaceUser == name);
            }
            var list = Code.ProductDataManage.Instance.QueryList(query, compileSp: true);
        }

        protected void Button2_Click(object sender, EventArgs e)
        {
            Code.ProductDataManage.Instance.QueryDayProduct(DateTime.Parse("2014-06-23"));
        }
    }
}
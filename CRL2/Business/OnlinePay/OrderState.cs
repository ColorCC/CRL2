﻿using System;
using System.Data;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Linq;
using CoreHelper;
namespace CRL.Business.OnlinePay
{
	/// <summary>
	/// 订单状态
	/// </summary>
	public enum OrderStatus
	{
		/// <summary>
		/// 默认
		/// </summary>
		已提交=0,
		/// <summary>
		/// 超时
		/// </summary>
		已过期=1,
		/// <summary>
        ///已确认
		/// </summary>
		已确认=2,
        /// <summary>
        /// 已退款
        /// </summary>
        已退款 = 3
	}
}

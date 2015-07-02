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
using System.Collections.Generic;
namespace CRL.Business.OnlinePay.Company
{
	/// <summary>
	/// 支付公司类型抽象类
	/// </summary>
	public  abstract class CompanyBase
	{
        public string MerchantKey
        {
            get
            {
                return GetAbsUrl(ChargeConfig.GetConfigKey(ThisCompanyType, ChargeConfig.DataType.Key));
            }
        }

        public string MerchantId
        {
            get
            {
                return GetAbsUrl(ChargeConfig.GetConfigKey(ThisCompanyType, ChargeConfig.DataType.User));
            }
        }
        public string ReturnUrl
        {
            get
            {
                return GetAbsUrl(ChargeConfig.GetConfigKey(ThisCompanyType, ChargeConfig.DataType.ReturnUrl));
            }
        }
        public string NotifyUrl
        {
            get
            {
                return GetAbsUrl(ChargeConfig.GetConfigKey(ThisCompanyType, ChargeConfig.DataType.NotifyUrl));
            }
        }

        public static string CurrentHost
        {
            get
            {
                string url = HttpContext.Current.Request.Url.ToString();
                string[] arry = url.Split('/');
                string host = arry[2];
                string url1 = arry[0] + "//" + host;
                //todo 更改主机URL
                
                return url1;
            }
        }
        public static string GetAbsUrl(string url)
        {
            if (url.StartsWith("http"))
            {
                return url;
            }
            return CurrentHost + url;
        }
        /// <summary>
        /// 当前类型
        /// </summary>
        public abstract CompanyType ThisCompanyType { get; }
        static object lockObj = new object();
        /// <summary>
        /// 生成订单号
        /// </summary>
        /// <returns></returns>
        public virtual string CreateOrderId()
        {
            return DateTime.Now.ToString("yyMMddHHmmssffff");
        }
		/// <summary>
		/// 生成订单实例
		/// </summary>
		/// <param name="money"></param>
		/// <param name="user"></param>
		/// <param name="userType"></param>
		/// <returns></returns>
        public IPayHistory CreateOrder(decimal amount, string user)
        {
            if (amount <= 0)
            {
                throw new Exception("支付金额不能小于或等于0");
            }
            IPayHistory order = new IPayHistory();
            order.Amount = amount;
            order.CompanyType = ThisCompanyType;
            order.Status = OrderStatus.已提交;
            order.UserId = user;
            lock (lockObj)
            {
                order.OrderId = CreateOrderId();
            }
            return order;    
        }

		/// <summary>
		/// 在这里写日志
		/// </summary>
		/// <param name="context"></param>
		private void NotifyLog(HttpContext context)
		{
            string address = CoreHelper.RequestHelper.GetCdnIP();

            string content = "NotifyLog:" + ThisCompanyType;
			content += " IP:" + address + "\r\n";
			content += " REQUEST: " + context.Request.QueryString.ToString() + "\r\n";
			content += " POST: " + context.Request.Form.ToString();
            Log(content);
		}
		/// <summary>
		/// 接口回调,在这里处理信息
		/// </summary>
		/// <param name="context"></param>
        protected abstract string OnNotify(HttpContext context);
        /// <summary>
        /// 获取通知
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public string GetNotify(HttpContext context)
        {
            NotifyLog(context);
            return OnNotify(context);
        }
		/// <summary>
		/// 提交到数据库+
		/// 对于代理,需查询OrderType为Charge的记录
		/// </summary>
		/// <param name="order"></param>
		protected void BaseSubmit(IPayHistory order)
		{
            if (ThisCompanyType == CompanyType.None)
            {
                throw new Exception("接口初始化没有指定类型,请检查");
            }
            if (string.IsNullOrEmpty(order.OrderId))
            {
                throw new Exception("没有指定OrderId");
            }
            OnlinePayBusiness.Instance.Add(order);
            string log = string.Format("提交订单 {0} {1} {2} {3}", order.UserId, order.OrderId, order.Amount, order.OrderType);
            Log(log, false);
		}

		/// <summary>
		/// 产生记录并URL转向
		/// </summary>
		/// <param name="order"></param>
		public abstract void Submit(IPayHistory order);

		/// <summary>
		/// 确认订单,失败会插入方法缓存,下次会自动重新执行方法
		/// </summary>
		/// <param name="orderNumber"></param>
		/// <param name="companyType"></param>
        //protected internal Order Confirm(string orderNumber, CompanyType companyType, Type fromType)
        //{
        //    Order order = OrderAction.GetOrder(orderNumber, companyType);
        //    if (order != null)
        //    {
        //        OrderAction.Confirm(order, fromType);
        //    }
        //    return order;
        //}

        /// <summary>
        /// 确认订单,并核对通知过来的金额
        /// </summary>
        /// <param name="order"></param>
        /// <param name="fromType"></param>
        /// <param name="notifyAmount">通知的订单金额,如果没有填原始订单的金额</param>
        protected internal void Confirm(IPayHistory order, Type fromType,decimal notifyAmount)
        {
            if (order.Amount != notifyAmount)
            {
                string message = order.CompanyType + "订单金额和支付金额对不上:" + order.OrderId + " 订单金额:" + order.Amount + " 通知金额:" + notifyAmount;
                Log(message, true);
                throw new Exception(message);
            }
            string key = "IPayHistory_" + order.OrderId;
            if (!CoreHelper.ConcurrentControl.Check(key, 10))
            {
                Log("过滤重复通知确认:" + order.OrderId + " amount:" + order.Amount);
                return;
            }
            if (order.Status== OrderStatus.已确认 || order.Status == OrderStatus.已退款)
            {
                return;
            }
            OnlinePayBusiness.Instance.ConfirmByType(order, fromType);
            Log("确认订单:" + order.OrderId + " amount:" + order.Amount);
        }
		/// <summary>
		/// 调用接口检查支付是否成功
		/// 根据情况手动Confirm
		/// </summary>
		/// <param name="order"></param>
		public abstract bool CheckOrder(IPayHistory order,out string message);
		/// <summary>
		/// 转向页执行此方法
		/// 这里为了实现自定义转向
		/// </summary>
		/// <param name="order"></param>
		public void Redirect(IPayHistory order)
		{
			if (!string.IsNullOrEmpty(order.RedirectUrl))
			{
                HttpContext.Current.Response.Redirect(order.RedirectUrl + "?amount=" + order.Amount + "&orderId=" + order.OrderId + "&companyType=" + (int)order.CompanyType + "&ProductOrderId=" + order.ProductOrderId);
			}
		}

        /// <summary>
        /// 记录日志
        /// </summary>
        /// <param name="log"></param>
        public void Log(string log)
        {
            CoreHelper.EventLog.Log(log, ThisCompanyType.ToString(), false);
        }
        /// <summary>
        /// 记录日志并发送到服务器
        /// </summary>
        /// <param name="log"></param>
        /// <param name="sendToServer"></param>
        public void Log(string log, bool sendToServer)
        {
            Log(log);
            if (sendToServer)
            {
                EventLog.SendToServer(log, ThisCompanyType.ToString());
            }
        }
        /// <summary>
        /// 订单退款方法
        /// </summary>
        /// <param name="order"></param>
        protected internal void BaseRefundOrder(IPayHistory order)
        {
            ///未确认的订单不能退款
            if (order.Status != OrderStatus.已确认)
            {
                return;
            }
            OnlinePayBusiness.Instance.RefundOrder(order);
            Log("支付订单被退款:" + order.OrderId + " amount:" + order.Amount, true);
        }

        /// <summary>
        /// 退款,取消订单
        /// </summary>
        /// <param name="order"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public abstract bool RefundOrder(IPayHistory order, out string message);

        //public abstract bool CheckCancelOrder(Order order, out string message);
	}
}

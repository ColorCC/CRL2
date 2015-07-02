﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
namespace CRL.Business.OnlinePay.Company.Bill99
{
    public class ChargeNotify : MessageBase
    {
        public string merchantAcctId
        {
            get;
            set;
        }
        public string version
        {
            get;
            set;
        }
        public string language
        {
            get;
            set;
        }
        public string signType
        {
            get;
            set;
        }
        public string payType
        {
            get;
            set;
        }
        public string bankId
        {
            get;
            set;
        }
        public string orderId
        {
            get;
            set;
        }
        public string orderTime
        {
            get;
            set;
        }
        public string orderAmount
        {
            get;
            set;
        }
        public string dealId
        {
            get;
            set;
        }
        public string bankDealId
        {
            get;
            set;
        }
        public string dealTime
        {
            get;
            set;
        }
        public string payAmount
        {
            get;
            set;
        }
        public string fee
        {
            get;
            set;
        }
        public string ext1
        {
            get;
            set;
        }
        public string ext2
        {
            get;
            set;
        }
        public string payResult
        {
            get;
            set;
        }
        public string errCode
        {
            get;
            set;
        }
        
        public static ChargeNotify FromRequest(System.Collections.Specialized.NameValueCollection c)
        {
            var fields = typeof(ChargeNotify).GetProperties();
            var obj = new ChargeNotify();
            foreach (var item in fields)
            {
                item.SetValue(obj, c[item.Name], null);
            }
            return obj;
        }
        
    }
}

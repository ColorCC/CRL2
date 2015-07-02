﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoreHelper;
using System.Data;
using System.Collections;
namespace CRL.Account
{

    /// <summary>
    /// 帐号维护,区分不同的帐号类型和流水类型
    /// </summary>
    public class AccountBusiness<TType> : BaseProvider<IAccountDetail> where TType : class
    {
        protected override DBExtend dbHelper
        {
            get { return GetDbHelper<TType>(); }
        }

        public static AccountBusiness<TType> Instance
        {
            get { return new AccountBusiness<TType>(); }
        }
        /// <summary>
        /// 创建帐户
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public bool CreateAccount(IAccountDetail info)
        {
            Add(info);
            return true;
        }
        /// <summary>
        /// 获取账户类型,传入枚举
        /// </summary>
        /// <param name="account"></param>
        /// <param name="accountType"></param>
        /// <param name="transactionType"></param>
        /// <returns></returns>
        public IAccountDetail GetAccount(int account, Enum accountType, Enum transactionType)
        {
            return GetAccount(account.ToString(), accountType.ToInt(), transactionType.ToInt());
        }
        /// <summary>
        /// 取得帐户信息,没有则创建(实时)
        /// </summary>
        /// <param name="account"></param>
        /// <param name="accountType"></param>
        /// <param name="transactionType"></param>
        /// <returns></returns>
        public IAccountDetail GetAccount(string account, int accountType, int transactionType)
        {
            var info = QueryItem(b => b.Account == account && b.TransactionType == transactionType && b.AccountType == accountType);
            if (info == null)
            {
                info = new IAccountDetail();
                info.Account = account;
                info.AccountType = accountType;
                info.TransactionType = transactionType;
                CreateAccount(info);
            }
            return info;
        }
        static Dictionary<int, IAccountDetail> detailInfoCache = new Dictionary<int, IAccountDetail>();
        /// <summary>
        /// 获取帐户详细信息,按帐户ID
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public IAccountDetail GetAccountFromCache(int accountId)
        {
            if (detailInfoCache.ContainsKey(accountId))
            {
                return detailInfoCache[accountId];
            }
            IAccountDetail info = QueryItem(b => b.Id == accountId);
            if (info == null)
                return null;

            lock (lockObj)
            {
                if (!detailInfoCache.ContainsKey(accountId))
                {
                    detailInfoCache.Add(accountId, info);
                }
            }
            return info;
        }

        /// <summary>
        /// 根据帐户ID取得对应的帐号
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public string GetAccountNoFromCache(int accountId)
        {
            var info = GetAccountFromCache(accountId);
            if (info == null)
                return null;
            return info.Account;
        }
        /// <summary>
        /// 获账户ID
        /// </summary>
        /// <param name="account"></param>
        /// <param name="accountType"></param>
        /// <param name="transactionType"></param>
        /// <returns></returns>
        public int GetAccountId(int account, Enum accountType, Enum transactionType)
        {
            return GetAccountId(account.ToString(), accountType.ToInt(), transactionType.ToInt());
        }
        /// <summary>
        /// 取得帐户ID(从缓存)
        /// </summary>
        /// <param name="account"></param>
        /// <param name="accountType">帐号类型,用以区分不同渠道用户</param>
        /// <param name="transactionType"></param>
        /// <returns></returns>
        public int GetAccountId(string account,int accountType, int transactionType)
        {
            int id = 0;
            foreach (var item in detailInfoCache.Values)
            {
                if (item.Account == account && item.AccountType == accountType&& item.TransactionType== transactionType)
                {
                    return item.Id;
                }
            }
            IAccountDetail detail = GetAccount(account, accountType, transactionType);
            lock (lockObj)
            {
                if (!detailInfoCache.ContainsKey(detail.Id))
                {

                    detailInfoCache.Add(detail.Id, detail);
                }
            }
            return detail.Id;
        }
    }
}

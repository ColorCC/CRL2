﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace CRL.Person
{
    /// <summary>
    /// 会员/人维护
    /// </summary>
    public class PersonBusiness<TType, TModel> : BaseProvider<TModel>
        where TType : class
        where TModel : Person, new()
    {
        public static PersonBusiness<TType, TModel> Instance
        {
            get { return new PersonBusiness<TType, TModel>(); }
        }
        protected override DBExtend dbHelper
        {
            get { return GetDbHelper<TType>(); }
        }
        /// <summary>
        /// 加密方法,默认MD5,如不同请重写
        /// </summary>
        /// <param name="passWord"></param>
        /// <returns></returns>
        public virtual string EncryptPass(string passWord)
        {
            if (SettingConfig.RoleAuthorizeEncryptPass != null)
            {
                return SettingConfig.RoleAuthorizeEncryptPass(passWord);
            }
            return CoreHelper.StringHelper.EncryptMD5(passWord); 
        }
        /// <summary>
        /// 检测帐号是否存在
        /// </summary>
        /// <param name="accountNo"></param>
        /// <returns></returns>
        public bool CheckAccountExists(string accountNo)
        {
            var item = QueryItem(b => b.AccountNo == accountNo);
            return item != null;
        }
        /// <summary>
        /// 验证密码
        /// </summary>
        /// <param name="accountNo"></param>
        /// <param name="passWord"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public bool CheckPass(string accountNo, string passWord, out string message)
        {
            message = "";
            accountNo = accountNo.Trim();
            var item = QueryItem(b => b.AccountNo == accountNo);
            if (item == null)
            {
                message = "帐号不存在";
                return false;
            }
            item.PassWord = item.PassWord + "";
            bool a = item.PassWord.ToUpper() == EncryptPass(passWord).ToUpper();
            if (!a)
            {
                message = "密码不正确";
            }
            if (item.Locked)
            {
                message = "帐号已被锁定";
                return false;
            }
            return a;
        }
        /// <summary>
        /// 更改密码
        /// </summary>
        /// <param name="accountNo"></param>
        /// <param name="passWord"></param>
        public void UpdatePass(string accountNo, string passWord)
        {
            ParameCollection c2 = new ParameCollection();
            c2["PassWord"] = EncryptPass(passWord);
            Update(b => b.AccountNo == accountNo, c2);
        }
        /// <summary>
        /// 修改付款密码
        /// </summary>
        /// <param name="accountNo"></param>
        /// <param name="passWord"></param>
        public void UpdatePayPass(string accountNo, string passWord)
        {
            ParameCollection c2 = new ParameCollection();
            c2["PayPass"] = EncryptPass(passWord);
            Update(b => b.AccountNo == accountNo, c2);
        }
        #region 登录验证
        /// <summary>
        /// 使用当前用户写入登录票据
        /// 写入值为 Id,Name,RuleName,TagData
        /// </summary>
        /// <param name="user"></param>
        /// <param name="rules">用户组</param>
        /// <param name="expires">是否自动过期</param>
        public void Login(TModel user, string rules, bool expires)
        {
            user.RuleName = rules;
            //要设置域请在WEB.CONFIG设置
            System.Web.Security.FormsAuthentication.SignOut();
            CoreHelper.FormAuthentication.AuthenticationSecurity.SetTicket(user, rules, expires);
        }
        /// <summary>
        /// 登出
        /// </summary>
        public void LoginOut()
        {
            CoreHelper.FormAuthentication.AuthenticationSecurity.LoginOut();
        }
        /// <summary>
        /// 检查登录票据
        /// Application_AuthenticateRequest使用
        /// </summary>
        public void CheckTicket()
        {
            CoreHelper.FormAuthentication.AuthenticationSecurity.CheckTicket();
        }
        /// <summary>
        /// 获取当前登录的用户
        /// 请用ID取详细信息
        /// </summary>
        public IPerson CurrentUser
        {
            get
            {
                string userTicket = System.Web.HttpContext.Current.User.Identity.Name;
                if (string.IsNullOrEmpty(userTicket))
                    return null;
                //数据不对会造成空
                CoreHelper.FormAuthentication.IUser user = new IPerson().ConverFromArry(userTicket);
                if (user == null)
                {
                    return null;
                }
                return user as IPerson;
            }
        }
        #endregion

        /// <summary>
        /// 记录登录日志
        /// </summary>
        /// <param name="log"></param>
        public virtual void SaveLoginLog(LoginLog log)
        {
            var helper = dbHelper;
            try
            {
                helper.InsertFromObj(log);
            }
            catch { }
        }
    }
}

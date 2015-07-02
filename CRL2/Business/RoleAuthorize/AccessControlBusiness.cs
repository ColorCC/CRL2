﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CRL.RoleAuthorize
{
    public class AccessControlBusiness : BaseProvider<AccessControl>
    {
        public static AccessControlBusiness Instance
        {
            get { return new AccessControlBusiness(); }
        }
        protected override DBExtend dbHelper
        {
            get { return GetDbHelper<AccessControlBusiness>(); }
        }
        public new string CreateTable()
        {
            DBExtend helper = dbHelper;
            AccessControl obj1 = new AccessControl();
            string msg = obj1.CreateTable(helper);
            Employee obj2 = new Employee();
            msg += obj2.CreateTable(helper);
            var obj3 = new Menu();
            msg += obj3.CreateTable(helper);
            var obj4 = new Role();
            msg += obj4.CreateTable(helper);
            var obj5 = new SystemType();
            msg += obj5.CreateTable(helper);
            var obj6 = new CRL.Person.LoginLog();
            msg += obj6.CreateTable(helper);
            SystemTypeBusiness.Instance.Add(new SystemType() { Name = "默认项目" });
            return msg;
        }
        //static List<AccessControl> accessControls;
        //public static List<AccessControl> AccessControls
        //{
        //    get
        //    {
        //        if (accessControls == null)
        //        {
        //            var helper = dbHelper;
        //            accessControls = helper.QueryList<AccessControl>();
        //        }
        //        return accessControls;
        //    }
        //}
        /// <summary>
        /// 查询用户的访问权限
        /// </summary>
        /// <param name="systemTypeId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public bool CheckAccess(int systemTypeId,int userId)
        {
            var menu = MenuBusiness.Instance.GetMenuByUrl(systemTypeId);
            if (menu == null)//默认没有的菜单都有权限
            {
                return true;
            }
            if (menu.ShowInNav)
            {
                #region 常用菜单
                Dictionary<string, int> dic = MenuBusiness.Instance.GetFavMenuDic(systemTypeId, userId);
                if (!dic.ContainsKey(menu.SequenceCode))
                {
                    dic.Add(menu.SequenceCode, 0);
                }
                dic[menu.SequenceCode] += 1;
                MenuBusiness.Instance.SaveFavMenus(dic, systemTypeId, userId);
                #endregion
            }
            var item = GetAccess(systemTypeId, menu.SequenceCode, userId);
            if (item == null)
                return false;
            var method = System.Web.HttpContext.Current.Request.HttpMethod;
            return item.Que;
        }
        /// <summary>
        /// 查询菜单操作权限
        /// </summary>
        /// <param name="systemTypeId"></param>
        /// <param name="userId"></param>
        /// <param name="op"></param>
        /// <returns></returns>
        public bool CheckAccess(int systemTypeId,int userId, MenuOperation op)
        {
            return CheckAccess(systemTypeId, userId);//暂不验证操作
        }
        AccessControl GetAccess(int systemTypeId, string menuCode, int userId)
        {
            var user = EmployeeBusiness.Instance.QueryItem(b => b.Id == userId);
            var item = QueryItem(b => ((b.Role == userId && b.RoleType == RoleType.用户) || (b.Role == user.Role && b.RoleType == RoleType.角色)) && b.SystemTypeId == systemTypeId && b.MenuCode == menuCode);
            return item;
        }
    }
}

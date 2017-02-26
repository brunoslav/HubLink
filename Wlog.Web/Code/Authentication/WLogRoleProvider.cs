﻿//******************************************************************************
// <copyright file="license.md" company="Wlog project  (https://github.com/arduosoft/wlog)">
// Copyright (c) 2016 Wlog project  (https://github.com/arduosoft/wlog)
// Wlog project is released under LGPL terms, see license.md file.
// </copyright>
// <author>Daniele Fontani, Emanuele Bucaelli</author>
// <autogenerated>true</autogenerated>
//******************************************************************************
using NHibernate;
using NHibernate.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;
using Wlog.Web.Code.Helpers;
using Wlog.BLL.Entities;
using Wlog.Library.BLL.Reporitories;
using NLog;

namespace Wlog.Web.Code.Authentication
{
    /// <summary>
    /// this class manage roles for logged users
    /// </summary>
    public class WLogRoleProvider : RoleProvider
    {

        private static Logger logger = LogManager.GetCurrentClassLogger();

        private const string ADMIN = "ADMIN";
        private const string USER = "USER";


        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            throw new NotImplementedException();
        }

        public override string ApplicationName
        {
            get
            {
                return "WLog";
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override void CreateRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            throw new NotImplementedException();
        }

        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            throw new NotImplementedException();
        }

        public override string[] GetAllRoles()
        {
            throw new NotImplementedException();
        }

        public override string[] GetRolesForUser(string username)
        {
            logger.Debug("[WLogRoleProvider]:GetRolesForUser");
            UserEntity u = RepositoryContext.Current.Users.GetByUsername(username);
            List<RolesEntity> roles = RepositoryContext.Current.Roles.GetAllRolesForUser(u);

            return roles.Select(x => x.RoleName).ToArray();
        }

        public override string[] GetUsersInRole(string roleName)
        {
            throw new NotImplementedException();
        }

        public override bool IsUserInRole(string username, string roleName)
        {
            logger.Debug("[WLogRoleProvider]:IsUserInRole");
            if (roleName == USER)
            {
                return true;
            }
            if (roleName == ADMIN)
            {

                UserEntity usr = RepositoryContext.Current.Users.GetByUsername(username);
                if (usr == null || !usr.IsAdmin)
                {
                    return false;
                }
                return true;
            }
            return false;
        }

        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            logger.Debug("[WLogRoleProvider]:RemoveUsersFromRoles");
            throw new NotImplementedException();
        }

        public override bool RoleExists(string roleName)
        {
            bool result = false;
            if (roleName == ADMIN || roleName == USER)
            {
                result = true;
            }
            return result;
        }
    }
}
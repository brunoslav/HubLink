﻿//******************************************************************************
// <copyright file="license.md" company="Wlog project  (https://github.com/arduosoft/wlog)">
// Copyright (c) 2016 Wlog project  (https://github.com/arduosoft/wlog)
// Wlog project is released under LGPL terms, see license.md file.
// </copyright>
// <author>Daniele Fontani, Emanuele Bucaelli</author>
// <autogenerated>true</autogenerated>
//******************************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Wlog.Web.Models;
using Wlog.Web.Code.Helpers;
using System.Web.Security;
using PagedList;
using Wlog.Web.Models.User;
using Wlog.Web.Code.Authentication;
using Wlog.Web.Models.Application;
using Wlog.BLL.Entities;
using Wlog.Library.BLL.Reporitories;
using Wlog.BLL.Classes;
using Wlog.Library.BLL.Classes;
using Wlog.Library.BLL.Enums;
using Wlog.Library.BLL.Helpers;
using Wlog.Library.BLL.Configuration;
using Wlog.Web.Filters;
using NLog;
using CronExpressionDescriptor;
using Wlog.Web.Code.Enums;

namespace Wlog.Web.Controllers
{
    /// <summary>
    /// this controller manage all features related with 
    /// </summary>
    public class PrivateController : Controller
    {

        private static Logger _logger = LogManager.GetCurrentClassLogger();

        [AuthorizeRolesAttribute(Constants.Roles.Admin, Constants.Roles.WriteLog, Constants.Roles.ReadLog)]
        public ActionResult Index()
        {

            _logger.Debug("[Private]: Index");
            string username = Membership.GetUser().UserName;
            var applicationIdsForUser = RepositoryContext.Current.Applications.GetAppplicationsByUsername(username);

            LogsSearchSettings logSearch = new LogsSearchSettings()
            {
                Applications = applicationIdsForUser.Select(x => x.Id).ToList(),
                PageNumber = 1,
                PageSize = 10
            };

            IPagedList<LogEntity> lastestLog = RepositoryContext.Current.Logs.SearchLog(logSearch);
            DashboardModel dm = new DashboardModel();
            dm.ErrorCount = RepositoryContext.Current.Logs.CountByLevel(StandardLogLevels.ERROR);
            dm.InfoCount = RepositoryContext.Current.Logs.CountByLevel(StandardLogLevels.INFO);
            dm.LogCount = RepositoryContext.Current.Logs.CountByLevel(StandardLogLevels.ALL_LEVELS);
            dm.WarnCount = RepositoryContext.Current.Logs.CountByLevel(StandardLogLevels.WARNING);
            dm.LastTen = ConversionHelper.ConvertLogEntityToMessage(lastestLog.ToList());
            dm.QueueLoad = LogQueue.Current.QueueLoad;
            dm.AppLastTen = new List<MessagesListModel>();

            IPagedList<LogEntity> logOfCurrentApp;

            foreach (ApplicationEntity application in applicationIdsForUser)
            {
                logSearch = new LogsSearchSettings()
                {
                    PageNumber = 1,
                    PageSize = 10

                };

                logSearch.Applications.Add(application.Id);
                logOfCurrentApp = RepositoryContext.Current.Logs.SearchLog(logSearch);
                MessagesListModel list = new MessagesListModel();
                list.ApplicationName = application.ApplicationName;
                list.IdApplication = application.Id;

                list.Messages = ConversionHelper.ConvertLogEntityToMessage(logOfCurrentApp.ToList());
                dm.AppLastTen.Add(list);
            }

            return View(dm);

        }

        [AuthorizeRolesAttribute(Constants.Roles.Admin, Constants.Roles.WriteLog, Constants.Roles.ReadLog)]
        public ActionResult Logs(Guid? applicationId, string level, string sortOrder, string sortBy, string serchMessage, int? page, int? pageSize)
        {
            _logger.Debug("[Private]: Logs");
            //TDOD: CHECK USER
            List<Guid> alloweApps = RepositoryContext.Current.Applications.GetAppplicationsIdsByUsername(Membership.GetUser().UserName);

            LogListModel mm = new LogListModel()
            {
                ApplicationId = applicationId ?? alloweApps.FirstOrDefault()
            };
            MembershipUser current = Membership.GetUser();
            mm.Apps = RepositoryContext.Current.Applications.GetAppplicationsByUsername(current.UserName);

            return View(mm);
        }


        public JsonResult Search(Guid? applicationId, string sortOrder, string sortBy, string serchMessage, int page, int pageSize)
        {
            _logger.Debug("[Private]: Search");
            //TDOD: CHECK USER
            IPagedList list = RepositoryContext.Current.Logs.GetLogsForApplication(Membership.GetUser().UserName, applicationId.Value,
                sortOrder, sortBy, serchMessage ?? "", pageSize, page);

            var result = new JsonResult();
            result.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            result.Data = new
            {
                draw = Request["draw"],
                recordsTotal = list.TotalItemCount,
                recordsFiltered = list.TotalItemCount,
                data = list
            };

            return result;
        }


        // Get  /Private/ListUsers
        [AuthorizeRolesAttribute(Constants.Roles.Admin)]
        public ActionResult ListUsers(string serchMessage, int? page, int? pageSize)
        {
            _logger.Debug("[Private]: ListUsers");
            ListUser model = new ListUser
            {
                SerchMessage = serchMessage
            };


            model.UserList = UserHelper.FilterUserList(serchMessage, page ?? 1, pageSize ?? 30);

            return View(model);
        }

        //Get Private/EditUser/1
        [HttpGet]
        [AuthorizeRolesAttribute(Constants.Roles.Admin)]
        public ActionResult EditUser(Guid id)
        {
            _logger.Debug("[Private]: EditUser({0})", id);
            UserEntity user = RepositoryContext.Current.Users.GetById(id);
            ViewBag.Title = user.Username;
            EditUser model = new EditUser();
            model.DataUser = user;
            model.Apps = UserHelper.GetApp(id);
            return View(model);
        }


        //Post Private/EditUser/
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(Constants.Roles.Admin)]
        public ActionResult EditUser(EditUser model)
        {
            _logger.Debug("[Private]: EditUser(model)");
            if (ModelState.IsValid)
            {
                try
                {
                    RepositoryContext.Current.Users.Save(model.DataUser);

                    List<AppUserRoleEntity> newRoleList = new List<AppUserRoleEntity>();

                    foreach (UserApps app in model.Apps)
                    {

                        if (app.RoleId != Guid.Empty)
                        {
                            newRoleList.Add(new AppUserRoleEntity { UserId = model.DataUser.Id, ApplicationId = app.IdApplication, RoleId = app.RoleId });
                        }
                    }

                    RepositoryContext.Current.Applications.ResetUserRoles(model.DataUser, newRoleList);


                    return RedirectToAction("ListUsers");
                }
                catch (Exception e)
                {
                    ModelState.AddModelError(String.Empty, "Error");
                }
            }
            ModelState.AddModelError(String.Empty, "Error");
            return View(model);
        }

        //Get Private/NewUser
        [HttpGet]
        [AuthorizeRoles(Constants.Roles.Admin)]
        public ActionResult NewUser()
        {
            _logger.Debug("[Private]: NewUser()");
            return View(new NewUser());
        }

        //Post Private/NewUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(Constants.Roles.Admin)]
        public ActionResult NewUser(NewUser user)
        {
            _logger.Debug("[Private]: NewUser(user)");
            if (ModelState.IsValid)
            {
                try
                {
                    MembershipCreateStatus status;
                    WLogMembershipProvider provider = new WLogMembershipProvider();
                    provider.CreateUser(user.UserName, user.Password, user.Email, null, null, true, null, out status);
                    if (status == MembershipCreateStatus.Success)
                    {
                        UserEntity entity = RepositoryContext.Current.Users.GetByUsername(user.UserName);
                        entity.IsAdmin = RepositoryContext.Current.Profiles.IsProfileIdAdmin(user.Profile);
                        entity.ProfileId = user.Profile;
                        RepositoryContext.Current.Users.Save(entity);
                        return RedirectToAction("EditUser", "Private", new { Id = entity.Id });
                    }
                    else
                    {
                        ModelState.AddModelError(String.Empty, ErrorCodeToString(status));
                    }

                }
                catch (MembershipCreateUserException e)
                {
                    ModelState.AddModelError(String.Empty, ErrorCodeToString(e.StatusCode));
                }
            }

            // Se si arriva a questo punto, significa che si è verificato un errore, rivisualizzare il form
            return View(user);
        }

        //Get Private/DeleteUser/1
        [HttpGet]
        [AuthorizeRoles(Constants.Roles.Admin)]
        public ActionResult DeleteUser(Guid id)
        {
            _logger.Debug("[Private]: DeleteUser({0})", id);

            UserEntity user = RepositoryContext.Current.Users.GetById(id);
            UserData result = new UserData
            {
                Id = user.Id,
                Email = user.Email,
                Username = user.Username,
                CreationDate = user.CreationDate,
                IsAdmin = user.IsAdmin,
                IsOnLine = user.IsOnLine,
                LastLoginDate = user.LastLoginDate
            };
            return View(result);
        }

        //Post Private/DeleteUser/1
        [HttpPost]
        [AuthorizeRoles(Constants.Roles.Admin)]
        public ActionResult DeleteUser(UserData user)
        {
            _logger.Debug("[Private]: DeleteUser(userdata)");

            if (RepositoryContext.Current.Users.DeleteUserById(user.Id))
            {
                return RedirectToAction("ListUsers");
            }
            else
            {
                ModelState.AddModelError(String.Empty, "Si è Verificato un Errore.");
            }

            return View(user);
        }


        #region Application
        // Get  /Private/ListApps
        [AuthorizeRoles(Constants.Roles.Admin, Constants.Roles.ReadLog)]
        public ActionResult ListApps(string serchMessage, int? page, int? pageSize)
        {
            _logger.Debug("[Private]: ListApps({0},{1},{2})", serchMessage, page, pageSize);
            string username = Membership.GetUser().UserName;

            ApplicationList model = new ApplicationList
            {
                SerchMessage = serchMessage
            };

            model.AppList = ApplicationHelper.FilterApplicationList(serchMessage, page ?? 1, pageSize ?? 30, username);

            return View(model);
        }

        //Get Private/NewApp
        [HttpGet]
        [AuthorizeRoles(Constants.Roles.Admin, Constants.Roles.CreateApp)]
        public ActionResult NewApp()
        {
            _logger.Debug("[Private]: NewApp()");
            return View(new ApplicationModel());
        }

        //Post Private/NewApp
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(Constants.Roles.Admin, Constants.Roles.CreateApp)]
        public ActionResult NewApp(ApplicationModel model)
        {
            _logger.Debug("[Private]: NewApp(model)");
            if (ModelState.IsValid)
            {
                ApplicationEntity entity = new ApplicationEntity();
                entity.ApplicationName = model.ApplicationName;
                entity.IsActive = true;
                entity.StartDate = model.StartDate;
                entity.PublicKey = model.PublicKey;
                entity.PublicKey = Guid.NewGuid();
                RepositoryContext.Current.Applications.Save(entity);

                UserEntity user = RepositoryContext.Current.Users.GetByUsername(Membership.GetUser().UserName);

                if (!user.IsAdmin)
                {
                    var role = RepositoryContext.Current.Roles.GetAllRolesForUser(user).SingleOrDefault(x => x.RoleName == Constants.Roles.CreateApp);
                    RepositoryContext.Current.Applications.AssignRoleToUser(entity, user, role);
                }

                return RedirectToAction("ListApps");
            }
            else
            {
                ModelState.AddModelError(String.Empty, "error");
            }

            return View(model);
        }


        //Get Private/EditApp/1
        [HttpGet]
        [AuthorizeRoles(Constants.Roles.Admin, Constants.Roles.CreateApp)]
        public ActionResult EditApp(Guid id)
        {
            _logger.Debug("[Private]: EditApp({0})", id);
            return View(ApplicationHelper.GetById(id));
        }


        //Post Private/EditApp/
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(Constants.Roles.Admin, Constants.Roles.CreateApp)]
        public ActionResult EditApp(ApplicationModel model)
        {
            _logger.Debug("[Private]: EditApp(model)");
            if (ModelState.IsValid)
            {
                ApplicationEntity entity = RepositoryContext.Current.Applications.GetById(model.IdApplication);
                entity.ApplicationName = model.ApplicationName;
                entity.IsActive = model.IsActive;
                entity.StartDate = model.StartDate;
                entity.EndDate = model.EndDate;
                //entity.PublicKey=model.PublicKey; Do not change this is not editable
                RepositoryContext.Current.Applications.Save(entity);
                return RedirectToAction("ListApps");
            }
            else
            {
                ModelState.AddModelError(String.Empty, "Error");
            }

            return View(model);
        }

        //Get Private/DeleteApp/1
        [HttpGet]
        [AuthorizeRoles(Constants.Roles.Admin, Constants.Roles.CreateApp)]
        public ActionResult DeleteApp(Guid id)
        {
            _logger.Debug("[Private]: DeleteApp({0})", id);
            return View(ApplicationHelper.GetById(id));
        }

        //Post Private/DeleteApp/1
        [HttpPost]
        [AuthorizeRolesAttribute(Constants.Roles.Admin, Constants.Roles.CreateApp)]
        public ActionResult DeleteApp(ApplicationModel model)
        {
            _logger.Debug("[Private]: DeleteApp(model)");

            if (ApplicationHelper.DeleteById(model.IdApplication))
            {
                return RedirectToAction("ListApps");
            }
            else
            {
                ModelState.AddModelError(String.Empty, "Unexpected exception");
            }
            return View(model);
        }

        #endregion

        #region Helper
        private static string ErrorCodeToString(MembershipCreateStatus createStatus)
        {
            _logger.Debug("[Private]: ErrorCodeToString({0})", createStatus);
            // see http://go.microsoft.com/fwlink/?LinkID=177550 
            switch (createStatus)
            {
                case MembershipCreateStatus.DuplicateUserName:
                    return Resources.Private.DuplicateUserName;

                case MembershipCreateStatus.DuplicateEmail:
                    return Resources.Private.DuplicateEmail;

                case MembershipCreateStatus.InvalidPassword:
                    return Resources.Private.InvalidPassword;

                case MembershipCreateStatus.InvalidEmail:
                    return Resources.Private.InvalidEmail;

                case MembershipCreateStatus.InvalidAnswer:
                    return Resources.Private.InvalidAnswer;

                case MembershipCreateStatus.InvalidQuestion:
                    return Resources.Private.InvalidQuestion;

                case MembershipCreateStatus.InvalidUserName:
                    return Resources.Private.InvalidUserName;

                case MembershipCreateStatus.ProviderError:
                    return Resources.Private.ProviderError;

                case MembershipCreateStatus.UserRejected:
                    return Resources.Private.UserRejected;

                default:
                    return Resources.Private.UnknownAuthorizationError;
            }
        }
        #endregion

        #region Info

        //Get Private/info

        public ActionResult Info()
        {
            var model = InfoHelper.GetInfoPage(InfoPageConfigurator.Configuration);
            return View(model);
        }
        #endregion

        #region Background Jobs

        public ActionResult BackgroundJobs()
        {
            var models = RepositoryContext.Current.JobDefinition.GetAllDefinitionsAndInstances();

            return View(models);
        }

        [HttpPost]
        public ActionResult ManageBackgroundJobs(JobConfiguration item, ButtonCommands command)
        {
            try
            {
                ExpressionDescriptor.GetDescription(item.CronExpression);
                switch (command)
                {
                    case ButtonCommands.Run:
                        ConversionHelper.RunJobInstance(item);
                        break;
                    case ButtonCommands.Edit:
                        ConversionHelper.UpdateJobInstance(item);
                        break;
                    default:
                        break;
                }
            }
            catch (FormatException)
            {
                ModelState.AddModelError("Error", Resources.Private.InvalidCron);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                ModelState.AddModelError("Error", Resources.Private.ErrorJobManagement);
            }

            var models = RepositoryContext.Current.JobDefinition.GetAllDefinitionsAndInstances();
            return View("BackgroundJobs", models);
        }

        #endregion
    }
}

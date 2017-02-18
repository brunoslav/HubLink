﻿//******************************************************************************
// <copyright file="license.md" company="Wlog project  (https://github.com/arduosoft/wlog)">
// Copyright (c) 2016 Wlog project  (https://github.com/arduosoft/wlog)
// Wlog project is released under LGPL terms, see license.md file.
// </copyright>
// <author>Daniele Fontani, Emanuele Bucaelli</author>
// <autogenerated>true</autogenerated>
//******************************************************************************
using Hangfire;
using Hangfire.MemoryStorage;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Cfg.MappingSchema;
using NHibernate.Dialect;
using NHibernate.Mapping.ByCode;
using NHibernate.Tool.hbm2ddl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Wlog.Web.Code.Helpers;
using Wlog.BLL.Classes;
using Wlog.DAL.NHibernate.Helpers;
using Wlog.Library.BLL.Reporitories;
using Wlog.Library.BLL.Configuration;
using Wlog.Library.Scheduler;
using NLog;

namespace Wlog.Web
{  

    public class WebApiApplication : System.Web.HttpApplication
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        protected void Application_Start()
        {
            logger.Info("Application starts");

            logger.Info("Registering configuration");
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(System.Web.Http.GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);


            logger.Info("Apply schema changes");
            RepositoryContext.Current.System.ApplySchemaChanges();

            logger.Info("Setup info config");

            InfoPageConfigurator.Configure(c => 
            {
                c.ApplicationName = "Wlog";
                
            });

            logger.Info("Start background jobs");

            HangfireBootstrapper.Instance.Start();

            logger.Info("Setup index configuration");
            IndexRepository.BasePath = HttpContext.Current.Server.MapPath("~/App_Data/Index/");


            logger.Info("install missing data");
            SystemDataHelper.InsertRolesAndProfiles();
            SystemDataHelper.EnsureSampleData();

            logger.Info("application started");
        }

        protected void Application_End(object sender, EventArgs e)
        {
            logger.Info("application end");

            logger.Info("stopping HangfireBootstrapper");
            HangfireBootstrapper.Instance.Stop();


        }
    }
}
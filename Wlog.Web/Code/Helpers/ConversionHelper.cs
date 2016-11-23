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
using NLog;
using Wlog.BLL.Classes;
using Wlog.BLL.Entities;
using Wlog.Library.BLL.Reporitories;
using Wlog.Web.Models;
using System.Linq;

namespace Wlog.Web.Code.Helpers
{
    public class ConversionHelper
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        #region Entity To Model

        /// <summary>
        /// Convert ApplicationEntity to ApplicationHome
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static ApplicationHomeModel ConvertEntityToApplicationHome(ApplicationEntity entity)
        {
            logger.Debug("[ConversionHelper]: ConvertEntityToApplicationHome");
            ApplicationHomeModel result = new ApplicationHomeModel();
            result.Id = entity.IdApplication;
            result.ApplicationName = entity.ApplicationName;
            result.StartDate = entity.StartDate;
            result.IsActive = entity.IsActive;
            return result;
        }



        /// <summary>
        /// Convert List<ApplicationEntity> to List<ApplicationHome>
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static List<ApplicationHomeModel> ConvertListEntityToListApplicationHome(List<ApplicationEntity> entity)
        {
            logger.Debug("[ConversionHelper]: ConvertListEntityToListApplicationHome");
            List<ApplicationHomeModel> result = new List<ApplicationHomeModel>();
            foreach (ApplicationEntity app in entity)
            {
                result.Add(ConvertEntityToApplicationHome(app));
            }
            return result;
        }
        #endregion

        internal static LogEntity ConvertLog(LogMessage log)
        {
            logger.Debug("[ConversionHelper]: ConvertLog");
            LogEntity result = new LogEntity();
            result.Uid = Guid.NewGuid();
            result.Level = log.Level;
            result.Message = log.Message;
            result.SourceDate = log.SourceDate;

            result.UpdateDate = DateTime.Now;

            try
            {
                Guid searchGuid = new Guid(log.ApplicationKey);
                RepositoryContext.Current.Applications.GetById(searchGuid);
            }
            catch (Exception err)
            {
                logger.Error(err);
            }

            return result;
        }


        public static List<LogMessage> ConvertLogEntityToMessage(List<LogEntity> list)
        {
            logger.Debug("[ConversionHelper]: ConvertLogEntityToMessage");
            List<LogMessage> result = new List<LogMessage>();
            foreach (LogEntity le in list)
            {

                LogMessage lm = new LogMessage();

                lm.Level = le.Level;
                lm.Message = le.Message;
                lm.SourceDate = le.SourceDate;

                result.Add(lm);
            }
            return result;
        }

        public static List<JobsConfigurationModel> GetAllConfiguredJobs()
        {
            try
            {
                logger.Debug("[ConversionHelper]: GetAllConfiguredJobs");
                var jobDefintions = RepositoryContext.Current.JobDefinition.GetAllJobs();
                var jobInstances = RepositoryContext.Current.JobInstance.GetAllJobs();

                var result = (from a in jobDefintions
                              join b in jobInstances
                              on a.Id equals b.JobDefinitionID
                              select new JobsConfigurationModel()
                              {
                                  Active = b.Active,
                                  CronExpression = b.CronExpression,
                                  Description = a.Description,
                                  JobName = a.Name,
                                  JobInstanceId = b.Id
                              }).ToList();

                return result;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return new List<JobsConfigurationModel>();
            }
        }

        public static bool UpdateJobInstance(JobsConfigurationModel jobModel)
        {
            try
            {
                var jobInstance = RepositoryContext.Current.JobInstance.GetJobInstanceById(jobModel.JobInstanceId);

                if (jobInstance == null)
                {
                    return false;
                }

                jobInstance.Active = jobModel.Active;
                jobInstance.CronExpression = jobModel.CronExpression;

                RepositoryContext.Current.JobInstance.Save(jobInstance);
                return true;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return false;
            }
        }
    }
}
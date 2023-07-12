using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using WebApi.Entities;
using WebApi.Helpers;

using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Options;
using System.IO;
using System.Net.Mail;
using CsvHelper;
using System.Text;
using System.Globalization;
using System.Net.Mime;
using Microsoft.Extensions.Logging;
using WebApi.Models.Messaging;
using WebApi.Repositories;

namespace WebApi.Services
{      
    public interface IMailService
    {
        Task<Mail> CreateResendMail(Mail originMail, List<MailAttachment> originMailAttachments);

        Task<MailsPageObj> GetMails(int userId, int folderId, int pageNumber, int rowsOfPage, string search);

        Task<List<MailModel>> GetMailsFolder(int userId, int folderId);

        Task<List<MailModel>> GetMailsLabel(int userId, int labelId);

        Task<ResendMailResponseModel> ResendMail(int userId, string mailId);
    }

    public class MailService : IMailService
    {               
        private readonly ILogger _log;
        private readonly IMailRepositories _mailRepositories;

        public MailService(DataContext context, ILogger<MailService> log
            , IMailRepositories mailRepositories)
        {                     
            _log = log;
            _mailRepositories = mailRepositories;
        }        
               
        public async Task<ResendMailResponseModel> ResendMail(int userId, string mailId)
        {
            try
            {
                return await _mailRepositories.ResendMail(userId, mailId);
            }
            catch (Exception ex)
            {
                _log.LogError(ex.Message);
                _log.LogError(ex.StackTrace);
                throw;
            }
        }
        public async Task<MailsPageObj> GetMails(int userId, int folderId, int pageNumber, int rowsOfPage, string search)
        {
            try
            {
                return await _mailRepositories.GetMails(userId, folderId, pageNumber, rowsOfPage, search);
            }
            catch (Exception ex)
            {
                _log.LogError(ex.Message);
                _log.LogError(ex.StackTrace);
                throw;
            }
        }      
        public async Task<Mail> CreateResendMail(Mail originMail, List<MailAttachment> originMailAttachments)
        {
            try
            {
                return await _mailRepositories.CreateResendMail(originMail, originMailAttachments);
            }
            catch (Exception ex)
            {
                _log.LogError(ex.Message);
                _log.LogError(ex.StackTrace);
                throw;
            }
        }

        public async Task<List<MailModel>> GetMailsLabel(int userId, int labelId)
        {
            try
            {
                return await _mailRepositories.GetMailsLabel(userId, labelId);
            }
            catch (Exception ex)
            {
                _log.LogError(ex.Message);
                _log.LogError(ex.StackTrace);

                throw;
            }
        }

        public async Task<List<MailModel>> GetMailsFolder(int userId, int folderId)
        {
            try
            {
                return await _mailRepositories.GetMailsFolder(userId, folderId);
            }
            catch (Exception ex)
            {
                _log.LogError(ex.Message);
                _log.LogError(ex.StackTrace);
                throw;
            }
        }
                
        

    }
}
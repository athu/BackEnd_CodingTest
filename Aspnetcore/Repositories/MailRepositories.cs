using Autofac.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection.Metadata;
using System;
using WebApi.Helpers;
using WebApi.Services;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using WebApi.Entities;
using WebApi.Models.Messaging;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace WebApi.Repositories
{
    public interface IMailRepositories
    {
        Task<Mail> CreateResendMail(Mail originMail, List<MailAttachment> originMailAttachments);

        Task<MailsPageObj> GetMails(int userId, int folderId, int pageNumber, int rowsOfPage, string search);

        Task<List<MailModel>> GetMailsFolder(int userId, int folderId);

        Task<List<MailModel>> GetMailsLabel(int userId, int labelId);

        Task<ResendMailResponseModel> ResendMail(int userId, string mailId);

    }
    public class MailRepositories : IMailRepositories, IDisposable
    {
        private DataContext _context;
        private readonly AppSettings _appSettings;
        private ILogger _log;

        // Flag: Has Dispose already been called?
        bool disposed = false;
        // Instantiate a SafeHandle instance.
        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);

        public MailRepositories(DataContext context, IOptions<AppSettings> appSettings, ILogger<MailRepositories> log)
        {
            _context = context;
            _appSettings = appSettings.Value;
            _log = log;
        }              
       
        public async Task<MailsPageObj> GetMails(int userId, int folderId, int pageNumber, int rowsOfPage, string search)
        {           
            MailsPageObj result = new MailsPageObj();
            List<Mail> mails = new List<Mail>();

            if (folderId == (int)CommonEnum.MailFolder.Received)
            {
                if (!string.IsNullOrEmpty(search))
                {
                    mails = await _context.Mail.Where(m => m.SendingUserID == userId &&
                    (EF.Functions.Like(m.ReceivingUser.StaffEmail, "%" + search + "%") || 
                    EF.Functions.Like(m.Subject, "%" + search + "%") ||
                    EF.Functions.Like(m.Message, "%" + search + "%"))).ToListAsync();
                }
                else
                {
                    mails = await _context.Mail.Where(m => m.SendingUserID == userId).ToListAsync();
                }

            } else
            {
                if (!string.IsNullOrEmpty(search))
                {
                    mails = await _context.Mail.Where(m => m.ReceivingUserID == userId &&
                    (EF.Functions.Like(m.SendingUser.StaffEmail, "%" + search + "%") || 
                    EF.Functions.Like(m.Subject, "%" + search + "%") ||
                    EF.Functions.Like(m.Message, "%" + search + "%"))).ToListAsync();
                }
                else
                {
                    mails = await _context.Mail.Where(m => m.ReceivingUserID == userId).ToListAsync();
                }
            }                                          
            
            int totalRows = mails.Count;
            mails = Utility.Pagination(mails, pageNumber, rowsOfPage);

            var mailsModel = (from a in mails
                              join s in _context.Users on a.SendingUserID equals s.UserID
                              join r in _context.Users on a.ReceivingUserID equals r.UserID
                              select new MailModel()
                              {
                                  Folder = a.Folder,
                                  HasAttachments = a.HasAttachments,
                                  Id = a.Id,
                                  Important = a.Important,
                                  Label = a.Label,
                                  Message = a.Message,
                                  Read = a.Read,
                                  ReceivingStaffEmail = r.StaffEmail,
                                  ReceivingStaffName = r.StaffName,
                                  SendingStaffEmail = s.StaffEmail,
                                  SendingStaffName = s.StaffName,
                                  SentSuccessToSMTPServer = a.SentSuccessToSMTPServer,
                                  SentTime = a.SentTime,
                                  Starred = a.Starred,
                                  Subject = a.Subject,
                              }).ToList();


            result.results = mailsModel;
            result.totalRows = totalRows;
            result.pageNumber = pageNumber;
            result.rowsOfPage = rowsOfPage;

            return result;
        }
        public async Task<Mail> CreateResendMail(Mail originMail, List<MailAttachment> originMailAttachments)
        {
            Mail newMail = new Mail();
            List<MailAttachment> newMailAttachments = new List<MailAttachment>();

            newMail._appSettings = originMail._appSettings;
            newMail._log = originMail._log;

            newMail.OriginMailID = originMail.Id;
            newMail.ReceivingUser = originMail.ReceivingUser;
            newMail.SendingUser = originMail.SendingUser;

            newMail.Label = originMail.Label;
            newMail.SentTime = DateTime.Now;
            newMail.SendingUserID = originMail.SendingUserID;
            newMail.ReceivingUserID = originMail.ReceivingUserID;
            newMail.Subject = originMail.Subject;
            newMail.Message = originMail.Message;
            newMail.HasAttachments = originMail.HasAttachments;
                       
            await AddtoDB(newMail);
            await CommittoDB(newMail);

            if (newMail.HasAttachments)
            {
                foreach (var a in originMailAttachments)
                {
                    string filePath = a.SavedPath;
                    string fileName = a.Filename;
                    Attachment attachment = new Attachment(filePath);
                    attachment.Name = fileName;
                    newMail.attachments.Add(attachment);

                    MailAttachment mailAttachment = new MailAttachment();
                    mailAttachment.MailID = newMail.Id;
                    mailAttachment.Filename = fileName;
                    mailAttachment.SavedPath = filePath;
                    newMailAttachments.Add(mailAttachment);
                }
            }

            await CommittoDB(newMailAttachments);
           
            return newMail;
        }
        public Task<List<MailModel>> GetMailsFolder(int userId, int folderId)
        {
            var userIdParam = new SqlParameter("@UserID", userId);
            var folderIdParam = new SqlParameter("FolderID", folderId);
            var results = _context.MailModel.FromSqlRaw(@"
                IF @FolderID = 0
                    select m.Id, su.StaffName as SendingStaffName, su.StaffEmail as SendingStaffEmail, ru.StaffName as ReceivingStaffName,
                    ru.StaffEmail as ReceivingStaffEmail, m.Subject, m.Message, m.SentTime, m.SentSuccessToSMTPServer, m.[Read], m.Starred, m.Important, m.HasAttachments, m.[Label], @FolderID as 'Folder'
                    --, m.Folder
                    from Mail m
                    left join Users su on m.SendingUserID = su.UserID
                    left join Users ru on m.ReceivingUserID = ru.UserID
                    where m.SendingUserID = @UserID
                    order by m.SentTime desc

                ELSE
                    select m.Id,
                    su.StaffName as SendingStaffName, su.StaffEmail as SendingStaffEmail, ru.StaffName as ReceivingStaffName, ru.StaffEmail as ReceivingStaffEmail,	m.Subject, m.Message, m.SentTime, m.SentSuccessToSMTPServer, m.[Read],	m.Starred, m.Important,	m.HasAttachments, m.[Label], @FolderID as 'Folder'
                    --, m.Folder
                    from Mail m
                    left join Users su on su.userID = m.SendingUserID
                    left join Users ru on ru.userID = m.ReceivingUserID
                    where m.ReceivingUserID = @UserID
                    order by m.SentTime desc", parameters: new[] { userIdParam, folderIdParam }).ToListAsync();

            return results;
        }

        public async Task<List<MailModel>> GetMailsLabel(int userId, int labelId)
        {
            var userIdParam = new SqlParameter("@UserID", userId);
            var labelIdParam = new SqlParameter("LabelID", labelId);
            var results = await _context.MailModel.FromSqlRaw(@"
                select m.Id, su.StaffName as SendingStaffName, su.StaffEmail as SendingStaffEmail, ru.StaffName as ReceivingStaffName, ru.StaffEmail as ReceivingStaffEmail, m.Subject, m.Message, m.SentTime, m.SentSuccessToSMTPServer, m.[Read], m.Starred, m.Important, m.HasAttachments, m.[Label], m.Folder 
                from Mail m
                left join Users su on su.UserID = m.SendingUserID
                left join Users ru on ru.UserID = m.ReceivingUserID
                where (m.SendingUserID = @UserID OR m.ReceivingUserID = @UserID)
                and m.Label = @LabelID
                order by m.SentTime desc", parameters: new[] { userIdParam, labelIdParam }).ToListAsync();
            return results;
        }

        public async Task<ResendMailResponseModel> ResendMail(int userId, string mailId)
        {
            ResendMailResponseModel result = new ResendMailResponseModel();

            // Begin transaction
            using var transaction = _context.Database.BeginTransaction();

            try
            {                
                Guid id = new Guid(mailId);
                List<MailAttachment> attachments = new List<MailAttachment>();

                var mailFound = await _context.Mail.Include(g => g.SendingUser).Include(g => g.ReceivingUser)
                                    .Where(g => g.Id == id && g.SendingUserID == userId).FirstOrDefaultAsync();

                if (mailFound != null)
                {
                    mailFound._appSettings = _appSettings;
                    mailFound._log = _log;

                    if (mailFound.HasAttachments)
                    {
                        attachments = _context.MailAttachments.Where(x => x.MailID == mailFound.Id).ToList();
                    }

                    Mail mailToSend = await this.CreateResendMail(mailFound, attachments);

                    int mailStatus = mailToSend.send();

                    if (mailStatus == 0 || mailStatus == -1)
                    {
                        transaction.Rollback();
                        result.ErrorMessage = "Failed to resend email.";
                        result.IsSuccess = false;
                        return result;
                    }

                    mailToSend.SentSuccessToSMTPServer = true;
                    await _context.SaveChangesAsync();
                    transaction.Commit();

                    result.IsSuccess = true;
                    return result;
                }
                else
                {
                    transaction.Rollback();
                    result.ErrorMessage = "You are not authorised to resend the email.";
                    result.IsSuccess = false;
                    return result;
                }
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                result.ErrorMessage = ex.Message;
                result.IsSuccess = false;
                return result;
            }
            
        }
       
        private async Task AddtoDB(Mail newMail)
        {
            try
            {
                await _context.Mail.AddAsync(newMail);
                return;
            }
            //XL add to catch Database update Exception
            catch (DbUpdateException ex)
            {

                throw new AppException(ex.InnerException.Message);
            }
            catch (AppException ex)
            {
                // return error message if there was an exception
                throw new AppException(ex.Message);
            }
        }

        private async Task CommittoDB(IEnumerable<MailAttachment> newMailAttachments)
        {
            try
            {
                await _context.MailAttachments.AddRangeAsync(newMailAttachments);
                await _context.SaveChangesAsync();
                return;
            }
            //XL add to catch Database update Exception
            catch (DbUpdateException ex)
            {

                throw new AppException(ex.InnerException.Message);
            }
            catch (AppException ex)
            {
                // return error message if there was an exception
                throw new AppException(ex.Message);
            }
        }

        private async Task CommittoDB(Object obj)
        {
            try
            {
                _context.Entry(obj).State = EntityState.Added;
                await _context.SaveChangesAsync();
                return;
            }
            //XL add to catch Database update Exception
            catch (DbUpdateException ex)
            {

                throw new AppException(ex.InnerException.Message);
            }
            catch (AppException ex)
            {
                // return error message if there was an exception
                throw new AppException(ex.Message);
            }
        }
                
        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                handle.Dispose();
                // Free any other managed objects here.
                //
                _context.Dispose();
            }

            disposed = true;
        }

        
    }
}

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using System.IdentityModel.Tokens.Jwt;
using WebApi.Helpers;
using Microsoft.Extensions.Options;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using WebApi.Services;
using WebApi.Entities;
using WebApi.Models.Users;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using Autofac.Util;
using Microsoft.Data.SqlClient;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System.IO;
using WebApi.Models.Messaging;
using System.Threading.Tasks;

namespace WebApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class MailsController : ControllerBase, IDisposable
    {
        // Flag: Has Dispose already been called?
        bool disposed = false;
        // Instantiate a SafeHandle instance.
        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);
        readonly Disposable _disposable;
        private IMailService _mailService;
        private IMapper _mapper;
        private readonly AppSettings _appSettings;
        private ILogger _log;
       
        public MailsController(
            IMailService mailService,
            IMapper mapper,
            ILogger<MailsController> log,
            IOptions<AppSettings> appSettings,
            DataContext context)
        {
            _mailService = mailService;
            _mapper = mapper;
            _log = log;
            _appSettings = appSettings.Value;           
            _disposable = new Disposable();
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
            }

            disposed = true;
        }


        [AllowAnonymous]
        [HttpGet]
        public IActionResult GetAll()
        {
            return null;
        }
               
        [HttpGet("folder/{paramFolderId}")]
        public async Task<IActionResult> GetFolder([FromRoute] int paramFolderId)
        {           
            try
            {
                HttpContext.Response.RegisterForDispose(_disposable);
                var userId = UserService.GetUserIdFromToken(Request.Headers["Authorization"], _appSettings.Secret);

                var result = await _mailService.GetMailsFolder(userId, paramFolderId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            
        }

       
        [HttpGet("label/{paramLabelId}")]
        public async Task<IActionResult> GetLabel([FromRoute] int paramLabelId)
        {
            try
            {
                HttpContext.Response.RegisterForDispose(_disposable);
                var userId = UserService.GetUserIdFromToken(Request.Headers["Authorization"], _appSettings.Secret);
                
                var result = await _mailService.GetMailsLabel(userId, paramLabelId);

                return Ok(result);
            }
            catch (Exception ex)
            {

                return BadRequest(new { message = ex.Message });
            }           
        }

        [HttpPost("m/{paramMailId}/resend")]
        public async Task<IActionResult> ResendMail([FromRoute]string paramMailId)
        {           
            try
            {
                HttpContext.Response.RegisterForDispose(_disposable);
                var userId = UserService.GetUserIdFromToken(Request.Headers["Authorization"], _appSettings.Secret);

                var result = await _mailService.ResendMail(userId, paramMailId);
                if(result.IsSuccess)
                {
                    return Ok(result);
                }                
                else
                {
                    return BadRequest(new { message = result.ErrorMessage });
                }                  
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
       
        [AllowAnonymous]
        [HttpGet("folderData/{paramFolderId}/{pageNumber}/{rowsOfPage}")]
        public async Task< IActionResult> GetMailFolder([FromRoute] int paramFolderId, [FromRoute] int pageNumber,  [FromRoute] int rowsOfPage,
            [FromQuery] string search)
        {                     
            try
            {
                var userId = UserService.GetUserIdFromToken(Request.Headers["Authorization"], _appSettings.Secret);
                
                var result = await _mailService.GetMails(userId, paramFolderId, pageNumber, rowsOfPage, search);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

           
    }
}

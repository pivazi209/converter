using api.Filters;
using api.Services;
using api.Utilities;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using System.Net;
using System.Web;

namespace api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConvertController : ControllerBase
    {
        private readonly ILogger<ConvertController> logger;
        private readonly IMessageSender messageSender;
        private readonly string uploadedPath;
        private readonly string convertedPath;

        public ConvertController(
            IConfiguration config,
            ILogger<ConvertController> logger,
            IMessageSender messageSender)
        {
            this.logger = logger;
            this.uploadedPath = config.GetValue<string>("uploadedPath");
            this.convertedPath = config.GetValue<string>("convertedPath");
            this.messageSender = messageSender;
        }

        [HttpPost, Route("html"), AllowOrigin]
        public async Task<IActionResult> PostHtml()
        {
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                return BadRequest();
            }

            var boundary = MultipartRequestHelper.GetBoundary(MediaTypeHeaderValue.Parse(Request.ContentType));
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);
            var section = await reader.ReadNextSectionAsync();

            var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(
                    section?.ContentDisposition, out var contentDisposition);

            if (!hasContentDispositionHeader 
             || !MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
            {
                return BadRequest();
            }

            var fileNameForFileStorage = Guid.NewGuid().ToString();
            var filePath = Path.GetFullPath(uploadedPath);

            using (var targetStream = System.IO.File.Create(
                Path.Combine(filePath, fileNameForFileStorage)))
            {
                var fileStream = section?.AsFileSection()?.FileStream;
                await fileStream?.CopyToAsync(targetStream);
            }

            messageSender.SendMessage(fileNameForFileStorage);

            return Ok(fileNameForFileStorage);
        }

        [HttpGet, Route("{guid}/exists"), AllowOrigin]
        public async Task<IActionResult> CheckExisting(Guid guid)
        {
            var filePath = Path.GetFullPath(convertedPath);
            var provider = new PhysicalFileProvider(filePath);
            var fileInfo = provider.GetFileInfo(Path.Combine(guid.ToString() + ".pdf"));

            return Ok(new
            {
                exists = fileInfo.Exists
            });
        }

        [HttpGet, Route("{guid}/download")]
        public async Task PdfDownload(Guid guid)
        {
            var filePath = Path.GetFullPath(convertedPath);
            var provider = new PhysicalFileProvider(filePath);
            var fileInfo = provider.GetFileInfo(Path.Combine(guid.ToString() + ".pdf"));

            Response.ContentType = "application/pdf";
            Response.Headers.Append("Content-Disposition", $"attachment; filename={fileInfo.Name}");
            await Response.SendFileAsync(fileInfo);
        }
    }
}
using System.Text;
using System.Web;
using DiscUtils.Iso9660;
using Microsoft.AspNetCore.Mvc;
using PxeServices;

namespace FluentPxeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class IsoController(ILogger<IsoController> logger) : ControllerBase
    {
        private static string IsoDir => ConstSetting.ISO_ROOT;

        // 示例：/api/iso/browse?isoPath=xxx.iso&path=folder1/file.txt
        [HttpGet("browse")]
        public async Task<IActionResult> Browse([FromQuery] string isoPath, [FromQuery] string path = "")
        {
            isoPath = Path.Combine(IsoDir, isoPath);
            if (string.IsNullOrEmpty(isoPath)
             || !System.IO.File.Exists(isoPath))
            {
                logger.LogInformation("ISO文件不存在");
                return NotFound("ISO文件不存在");
            }

            var       normalizedPath = path?.Replace('\\', '/').Trim('/');
            var       isoStream      = System.IO.File.OpenRead(isoPath);
            using var cd             = new CDReader(isoStream, true);


            logger.LogInformation("ISO文件：{Path}", isoPath);

            // 目录或文件路径标准化
            logger.LogInformation("浏览路径：" + normalizedPath);
            if (string.IsNullOrEmpty(normalizedPath))
            {
                // 根目录
                if (!cd.DirectoryExists(""))
                {
                    logger.LogInformation("ISO镜像无根目录");
                    return NotFound("ISO镜像无根目录");
                }

                return Content(RenderDirectoryListing(cd, ""), "text/html; charset=utf-8");
            }

            if (cd.DirectoryExists(normalizedPath.Replace("/", "\\")))
            {
                var dirs = cd.GetDirectories(normalizedPath.Replace("/", "\\"));
                logger.LogInformation("ISO目录：{Dirs}", dirs);
                var files = cd.GetFiles(normalizedPath.Replace("/", "\\"));
                logger.LogInformation("文件：{Files}", files);
                // 目录浏览
                return Content(RenderDirectoryListing(cd, normalizedPath), "text/html; charset=utf-8");
            }

            if (cd.FileExists(normalizedPath.Replace("/", "\\")))
            {
                // 文件下载
                var isoFileStream = cd.OpenFile(normalizedPath.Replace("/", "\\"), FileMode.Open, FileAccess.Read);

                var fileName = Path.GetFileName(normalizedPath);
                return File(isoFileStream, "application/octet-stream", fileName);
            }

            logger.LogInformation("指定路径不存在于ISO镜像中");
            return NotFound("指定路径不存在于ISO镜像中");
        }

        private string RenderDirectoryListing(CDReader cd, string dirPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<html><head><title>Index of " + HttpUtility.HtmlEncode(dirPath) + "</title></head><body>");
            sb.AppendLine("<h1>Index of "                + HttpUtility.HtmlEncode(dirPath) + "</h1>");
            sb.AppendLine("<hr><pre>");

            // 上级目录
            if (dirPath != "/")
            {
                var parent = dirPath.TrimEnd('/');
                parent = parent.Contains('/') ? parent[..parent.LastIndexOf('/')] : "";
                if (string.IsNullOrEmpty(parent)) parent = "/";
                sb.AppendLine($"<a href=\"?isoPath={HttpUtility.UrlEncode(Request.Query["isoPath"])}&path={HttpUtility.UrlEncode(parent.Trim('/'))}\">../</a>");
            }

            // 目录
            foreach (var dir in cd.GetDirectories(dirPath.Replace("/", "\\")).OrderBy(d => d))
            {
                var name    = Path.GetFileName(dir.TrimEnd('/'));
                var relPath = (dir.TrimStart('/'));
                sb.AppendLine($"<a href=\"?isoPath={HttpUtility.UrlEncode(Request.Query["isoPath"])}&path={HttpUtility.UrlEncode(relPath)}\">{HttpUtility.HtmlEncode(name)}/</a>");
            }

            // 文件
            foreach (var file in cd.GetFiles(dirPath.Replace("/", "\\")).OrderBy(f => f))
            {
                var name    = Path.GetFileName(file);
                var relPath = (file.TrimStart('/'));
                sb.AppendLine($"<a href=\"?isoPath={HttpUtility.UrlEncode(Request.Query["isoPath"])}&path={HttpUtility.UrlEncode(relPath)}\">{HttpUtility.HtmlEncode(name)}</a>");
            }

            sb.AppendLine("</pre><hr></body></html>");
            return sb.ToString();
        }
    }
}
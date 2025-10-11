using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using PxeServices;

namespace FluentPxeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TftpFileController(TftpService tftpService, ILogger<TftpFileController> logger) : ControllerBase
    {
        /// <summary>
        /// 获取目录结构（包括文件和子目录）
        /// </summary>
        /// <param name="path">目录路径（相对路径），默认为根目录</param>
        /// <returns>目录结构信息</returns>
        [HttpGet("directory")]
        public IActionResult GetDirectoryStructure([FromQuery] string path = "")
        {
            try
            {
                logger.LogInformation("Getting directory structure for path: {Path}", path);
                var directoryStructure = tftpService.GetDirectoryStructure(path);
                return Ok(directoryStructure);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting directory structure for path: {Path}", path);
                return StatusCode(StatusCodes.Status500InternalServerError, "获取目录结构时发生错误");
            }
        }

        /// <summary>
        /// 下载TFTP目录中的文件
        /// </summary>
        /// <param name="fileName">文件名（包括相对路径）</param>
        /// <returns>文件内容</returns>
        [HttpGet("download/{*fileName}")]
        public async Task<IActionResult> DownloadFile([FromRoute] string fileName)
        {
            try
            {
                // 防止路径遍历攻击
                if (fileName.Contains(".."))
                {
                    logger.LogWarning("Attempted path traversal attack: {FileName}", fileName);
                    return BadRequest("文件名格式不正确");
                }

                // 获取完整文件路径
                var fullPath = Path.Combine(tftpService.RootDirectory, fileName);

                // 检查路径是否存在
                if (!Directory.Exists(fullPath)
                 && !System.IO.File.Exists(fullPath))
                {
                    logger.LogWarning("Path not found: {FullPath}", fullPath);
                    return NotFound("路径不存在");
                }

                // 如果是目录，返回目录列表
                if (Directory.Exists(fullPath))
                {
                    logger.LogInformation("Displaying directory listing for: {FullPath}", fullPath);
                    return Content(GenerateDirectoryListingHtml(fileName, fullPath), "text/html");
                }

                // 如果是文件，正常下载
                var fileInfo = new FileInfo(fullPath);
                var mimeType = GetMimeType(fileInfo.Extension);

                logger.LogInformation("Downloading file: {FileName}", fileName);

                // 返回文件流
                var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                Response.StatusCode = StatusCodes.Status200OK;
                return File(stream, mimeType, fileInfo.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing request for: {FileName}", fileName);
                return StatusCode(StatusCodes.Status500InternalServerError, "处理请求时发生错误");
            }
        }

        /// <summary>
        /// 生成类似Apache风格的目录列表HTML页面
        /// </summary>
        /// <param name="relativePath">相对路径</param>
        /// <param name="fullPath">完整路径</param>
        /// <returns>目录列表的HTML内容</returns>
        private string GenerateDirectoryListingHtml(string relativePath, string fullPath)
        {
            var directoryInfo = new DirectoryInfo(fullPath);
            var items         = directoryInfo.GetFileSystemInfos();

            // 排序：先目录，后文件，按名称排序
            var directories = items.Where(i => i is DirectoryInfo).OrderBy(i => i.Name);
            var files       = items.Where(i => i is FileInfo).OrderBy(i => i.Name);

            var htmlBuilder = new System.Text.StringBuilder();

            // 生成页面标题
            htmlBuilder.AppendLine("<!DOCTYPE html>");
            htmlBuilder.AppendLine("<html>");
            htmlBuilder.AppendLine("<head>");
            htmlBuilder.AppendLine("<title>Index of /api/tftpfile/download/" + relativePath + "</title>");
            htmlBuilder.AppendLine("<style>");
            htmlBuilder.AppendLine("    body { font-family: Arial, sans-serif; margin: 20px; }");
            htmlBuilder.AppendLine("    h1 { color: #333; }");
            htmlBuilder.AppendLine("    table { width: 100%; border-collapse: collapse; margin-top: 20px; }");
            htmlBuilder.AppendLine("    th, td { padding: 8px; text-align: left; border-bottom: 1px solid #ddd; }");
            htmlBuilder.AppendLine("    th { background-color: #f2f2f2; }");
            htmlBuilder.AppendLine("    tr:hover { background-color: #f5f5f5; }");
            htmlBuilder.AppendLine("    a { color: #0066cc; text-decoration: none; }");
            htmlBuilder.AppendLine("    a:hover { text-decoration: underline; }");
            htmlBuilder.AppendLine("    .dir { font-weight: bold; }");
            htmlBuilder.AppendLine("</style>");
            htmlBuilder.AppendLine("</head>");
            htmlBuilder.AppendLine("<body>");
            htmlBuilder.AppendLine("<h1>Index of /api/tftpfile/download/" + relativePath + "</h1>");

            // 生成返回上级目录的链接
            if (!string.IsNullOrEmpty(relativePath))
            {
                var parentPath                                   = Path.GetDirectoryName(relativePath);
                if (string.IsNullOrEmpty(parentPath)) parentPath = "/";
                htmlBuilder.AppendLine("<a href='/api/tftpfile/download/" + parentPath + "'>[To Parent Directory]</a><br/><br/>");
            }

            // 生成目录列表表格
            htmlBuilder.AppendLine("<table>");
            htmlBuilder.AppendLine("    <tr><th>Name</th><th>Last modified</th><th>Size</th></tr>");

            // 添加目录列表
            foreach (var dir in directories)
            {
                var dirName         = dir.Name;
                var dirRelativePath = string.IsNullOrEmpty(relativePath) ? dirName : Path.Combine(relativePath, dirName);
                var lastModified    = ((DirectoryInfo)dir).LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");

                htmlBuilder.AppendLine("    <tr>");
                htmlBuilder.AppendLine("        <td><a href='/api/tftpfile/download/" + dirRelativePath + "' class='dir'>" + dirName + "/</a></td>");
                htmlBuilder.AppendLine("        <td>"                                 + lastModified    + "</td>");
                htmlBuilder.AppendLine("        <td>-</td>");
                htmlBuilder.AppendLine("    </tr>");
            }

            // 添加文件列表
            foreach (var file in files)
            {
                var fileName         = file.Name;
                var fileRelativePath = string.IsNullOrEmpty(relativePath) ? fileName : Path.Combine(relativePath, fileName);
                var lastModified     = ((FileInfo)file).LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
                var fileSize         = FormatFileSize(((FileInfo)file).Length);

                htmlBuilder.AppendLine("    <tr>");
                htmlBuilder.AppendLine("        <td><a href='/api/tftpfile/download/" + fileRelativePath + "'>" + fileName + "</a></td>");
                htmlBuilder.AppendLine("        <td>"                                 + lastModified     + "</td>");
                htmlBuilder.AppendLine("        <td>"                                 + fileSize         + "</td>");
                htmlBuilder.AppendLine("    </tr>");
            }

            htmlBuilder.AppendLine("</table>");
            htmlBuilder.AppendLine("</body>");
            htmlBuilder.AppendLine("</html>");

            return htmlBuilder.ToString();
        }

        /// <summary>
        /// 格式化文件大小显示
        /// </summary>
        /// <param name="bytes">文件字节数</param>
        /// <returns>格式化后的文件大小字符串</returns>
        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            var      order = 0;
            double   size  = bytes;

            while (size  >= 1024
                && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }

        /// <summary>
        /// 获取文件的MIME类型
        /// </summary>
        /// <param name="extension">文件扩展名</param>
        /// <returns>MIME类型</returns>
        private string GetMimeType(string extension)
        {
            // 简单的MIME类型映射
            switch (extension.ToLower())
            {
                case ".txt":
                    return MediaTypeNames.Text.Plain;
                case ".bin":
                case ".exe":
                case ".iso":
                    return MediaTypeNames.Application.Octet;
                case ".jpg":
                case ".jpeg":
                    return MediaTypeNames.Image.Jpeg;
                case ".png":
                    return MediaTypeNames.Image.Png;
                case ".html":
                    return MediaTypeNames.Text.Html;
                case ".css":
                    return "text/css";
                case ".js":
                    return "application/javascript";
                default:
                    return MediaTypeNames.Application.Octet;
            }
        }
    }
}
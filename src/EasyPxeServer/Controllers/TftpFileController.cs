using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using EasyPxeServer.Services;
using System.IO;
using System.Net.Mime;
using System.Threading.Tasks;

namespace EasyPxeServer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TftpFileController(TFTPService tftpService, ILogger<TftpFileController> logger) : ControllerBase
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

                // 检查文件是否存在
                if (!System.IO.File.Exists(fullPath))
                {
                    logger.LogWarning("File not found: {FullPath}", fullPath);
                    return NotFound("文件不存在");
                }

                // 设置响应头，提供文件下载
                var fileInfo = new FileInfo(fullPath);
                var mimeType = GetMimeType(fileInfo.Extension);

                logger.LogInformation("Downloading file: {FileName}", fileName);

                // 返回文件流
                var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                return File(stream, mimeType, fileInfo.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error downloading file: {FileName}", fileName);
                return StatusCode(StatusCodes.Status500InternalServerError, "下载文件时发生错误");
            }
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
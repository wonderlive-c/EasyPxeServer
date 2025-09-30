using System.Net;
using Tftp.Net;

namespace EasyPxeServer.Services;

public class TFTPService : IHostedService
{
    private readonly ILogger<TFTPService> logger;
    private          TftpServer?          tftpServer;

    public bool IsRunning => tftpServer != null;

    public string RootDirectory { get; set; } = "tftpboot";

    public string SelectedInterfaceIp { get; set; } = "";

    public TFTPService(ILogger<TFTPService> logger)
    {
        this.logger = logger;
        // 确保TFTP根目录存在
        if (!Directory.Exists(RootDirectory)) Directory.CreateDirectory(RootDirectory);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (tftpServer != null)
            return;

        try
        {
            // 创建TFTP服务器
            var port = 69;
            tftpServer = !string.IsNullOrEmpty(SelectedInterfaceIp) ? new TftpServer(IPAddress.Parse(SelectedInterfaceIp), port) : new TftpServer(IPAddress.Any, port);

            // 处理读请求
            tftpServer.OnReadRequest += HandleReadRequest;
            // 处理写请求（如果需要）
            tftpServer.OnWriteRequest += HandleWriteRequest;

            tftpServer.Start();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start TFTP service: {Message}", ex.Message);
            tftpServer = null;
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (tftpServer == null)
            return;

        try
        {
            tftpServer.Dispose();
            tftpServer = null;
        }
        catch (Exception ex) { logger.LogError(ex, "Error stopping TFTP service: {Message}", ex.Message); }
    }

    private async void HandleReadRequest(ITftpTransfer transfer, EndPoint client)
    {
        try
        {
            var fileName = transfer.Filename;
            var fullPath = Path.Combine(RootDirectory, fileName);

            logger.LogInformation("TFTP Read request for file: {FileName} from {Client}", fileName, client);

            if (File.Exists(fullPath))
            {
                try
                {
                    var taskCompletionSource = new TaskCompletionSource<bool>();
                    transfer.OnProgress += (s, e) => { logger.LogInformation("TFTP Transfer progress: {Progress:P}", (double)e.TransferredBytes / e.TotalBytes); };

                    transfer.OnError += (s, e) =>
                    {
                        logger.LogError("TFTP Transfer error: {Error}", e);
                        taskCompletionSource.TrySetResult(false);
                    };
                    logger.LogInformation("Sending file: {FileName} to {Client}", fullPath, client);

                    await using var stream = File.OpenRead(fullPath);
                    transfer.Start(stream);
                    transfer.OnFinished += (s) =>
                    {
                        logger.LogInformation("TFTP Transfer complete for file: {FileName} to {Client}", fullPath, client);
                        taskCompletionSource.TrySetResult(true);
                    };

                    await Task.WhenAny(taskCompletionSource.Task, Task.Delay(TimeSpan.FromMinutes(10)));
                }
                catch (Exception e) { logger.LogError(e, "Error sending file: {Message}", e.Message); }
            }
            else
            {
                logger.LogWarning("File not found: {FullPath}", fullPath);
                transfer.Cancel(TftpErrorPacket.FileNotFound);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling TFTP read request: {Message}", ex.Message);
            transfer.Cancel(TftpErrorPacket.FileNotFound);
        }
    }

    private void HandleWriteRequest(ITftpTransfer transfer, EndPoint client)
    {
        try
        {
            var fileName = transfer.Filename;
            var fullPath = Path.Combine(RootDirectory, fileName);

            logger.LogInformation("TFTP Write request for file: {FileName} from {Client}", fileName, client);

            // 创建目录（如果不存在）
            var directory = Path.GetDirectoryName(fullPath);
            if (directory != null
             && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            transfer.OnProgress += (s, e) => { logger.LogInformation("TFTP Transfer progress: {Progress:P}", (double)e.TransferredBytes / e.TotalBytes); };

            transfer.OnError += (s, e) => { logger.LogError("TFTP Transfer error: {Error}", e); };

            using var stream = File.Create(fullPath);
            transfer.Start(stream);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling TFTP write request: {Message}", ex.Message);
            transfer.Cancel(TftpErrorPacket.FileNotFound);
        }
    }

    // 获取TFTP目录中的文件列表（仅顶层）
    public List<TftpFileInfo> GetFiles()
    {
        var files = new List<TftpFileInfo>();

        if (!Directory.Exists(RootDirectory))
            return files;

        try
        {
            foreach (var filePath in Directory.GetFiles(RootDirectory, "*.*", SearchOption.TopDirectoryOnly))
            {
                var fileInfo = new FileInfo(filePath);
                files.Add(new TftpFileInfo
                {
                    Name         = Path.GetRelativePath(RootDirectory, filePath),
                    Size         = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTime
                });
            }
        }
        catch (Exception ex) { logger.LogError(ex, "Error getting TFTP files: {Message}", ex.Message); }

        return files;
    }

    // 获取TFTP目录结构（包括子目录和文件）
    public List<DirectoryItem> GetDirectoryStructure(string path = "")
    {
        var    result = new List<DirectoryItem>();
        string fullPath;

        // 构建完整路径
        if (string.IsNullOrEmpty(path)) { fullPath = RootDirectory; }
        else
        {
            // 防止路径遍历攻击
            if (path.Contains(".."))
            {
                logger.LogWarning("Attempted path traversal attack: {Path}", path);
                return result;
            }

            fullPath = Path.Combine(RootDirectory, path);
        }

        if (!Directory.Exists(fullPath))
            return result;

        try
        {
            // 添加目录
            foreach (var dirPath in Directory.GetDirectories(fullPath))
            {
                var relativePath = Path.GetRelativePath(RootDirectory, dirPath);
                result.Add(new DirectoryItem
                {
                    Name         = Path.GetFileName(dirPath),
                    Path         = relativePath,
                    IsDirectory  = true,
                    Size         = 0,
                    LastModified = Directory.GetLastWriteTime(dirPath)
                });
            }

            // 添加文件
            foreach (var filePath in Directory.GetFiles(fullPath))
            {
                var fileInfo     = new FileInfo(filePath);
                var relativePath = Path.GetRelativePath(RootDirectory, filePath);
                result.Add(new DirectoryItem
                {
                    Name         = Path.GetFileName(filePath),
                    Path         = relativePath,
                    IsDirectory  = false,
                    Size         = fileInfo.Length,
                    LastModified = fileInfo.LastWriteTime
                });
            }
        }
        catch (Exception ex) { logger.LogError(ex, "Error getting directory structure for path: {Path}, Message: {Message}", path, ex.Message); }

        return result;
    }

    // 上传文件到TFTP目录
    public async Task<bool> UploadFileAsync(string fileName, Stream fileStream)
    {
        try
        {
            var fullPath = Path.Combine(RootDirectory, fileName);

            // 创建目录（如果不存在）
            var directory = Path.GetDirectoryName(fullPath);
            if (directory != null
             && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            await using var outputStream = File.Create(fullPath);
            await fileStream.CopyToAsync(outputStream);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error uploading file: {Message}", ex.Message);
            return false;
        }
    }

    // 删除TFTP目录中的文件
    public bool DeleteFile(string fileName)
    {
        try
        {
            var fullPath = Path.Combine(RootDirectory, fileName);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting file: {Message}", ex.Message);
            return false;
        }
    }
}

public class TftpFileInfo
{
    public string   Name         { get; set; } = string.Empty;
    public long     Size         { get; set; }
    public DateTime LastModified { get; set; }

    public string FormattedSize
    {
        get
        {
            if (Size < 1024)
                return $"{Size} B";
            if (Size < 1024 * 1024)
                return $"{Size / 1024.0:F1} KB";
            return $"{Size / (1024 * 1024.0):F1} MB";
        }
    }
}

// 目录项信息类
public class DirectoryItem
{
    public string   Name         { get; set; } = string.Empty;
    public string   Path         { get; set; } = string.Empty;
    public bool     IsDirectory  { get; set; }
    public long     Size         { get; set; }
    public DateTime LastModified { get; set; }

    public string FormattedSize
    {
        get
        {
            if (IsDirectory)
                return "-";
            if (Size < 1024)
                return $"{Size} B";
            if (Size < 1024 * 1024)
                return $"{Size / 1024.0:F1} KB";
            return $"{Size / (1024 * 1024.0):F1} MB";
        }
    }
}
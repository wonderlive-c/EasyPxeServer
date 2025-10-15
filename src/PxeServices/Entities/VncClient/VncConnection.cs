#region Copyright

// ===============================================================================
//   Project Name        :    PxeServices
//   Project Description :
//   ===============================================================================
//   File Name           :    VncConnection.cs
//   File Version        :    v1.0.0.0
//   File Description    :
//   Author              :    wonderlive
//   Create Time         :    2025-10-15 14:20
//   Update Time         :    2025-10-15 14:20
// 
//   ===============================================================================
//      _____  _____  / /_
//     / ___/ / ___/ / __/
//    (__  ) / /__  / /_
//   /____/  \___/  \__/     Copyright © 2025 SCT Tech. Co., Ltd. All rights reserved.
//   ===============================================================================

#endregion

using System.ComponentModel.DataAnnotations;

namespace PxeServices.Entities.VncClient;

/// <summary>
/// VNC连接信息类
/// </summary>
public class VncConnection : IEntity<Guid>
{
    [MaxLength(32)]
    public string ConnectionName { get; set; }

    [MaxLength(64)]
    public string ConnectionId { get; set; }

    [MaxLength(32)]
    public string Host { get; set; }

    public int Port { get; set; }

    [MaxLength(32)]
    public string Password { get; set; }

    [MaxLength(256)]
    public string WebSocketUrl { get; set; }

    public bool IsConnected { get; set; }

    [MaxLength(64)]
    public string DesktopName { get; set; }

    #region Implementation of IEntity<Guid>

    public Guid Id { get; set; }

    #endregion
}
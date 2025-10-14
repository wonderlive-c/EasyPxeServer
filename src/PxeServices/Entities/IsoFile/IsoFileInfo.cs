#region Copyright

// ===============================================================================
//   Project Name        :    PxeServices
//   Project Description :
//   ===============================================================================
//   File Name           :    IsoFileInfo.cs
//   File Version        :    v1.0.0.0
//   File Description    :
//   Author              :    wonderlive
//   Create Time         :    2025-10-14 17:22
//   Update Time         :    2025-10-14 17:22
// 
//   ===============================================================================
//      _____  _____  / /_
//     / ___/ / ___/ / __/
//    (__  ) / /__  / /_
//   /____/  \___/  \__/     Copyright © 2025 SCT Tech. Co., Ltd. All rights reserved.
//   ===============================================================================

#endregion

namespace PxeServices.Entities.IsoFile;

public interface IIsoFileInfo
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public string Path { get; set; }

    public long Size { get; set; }

    public DateTime CreateTime { get; set; }

    public DateTime ModifyTime { get; set; }
}
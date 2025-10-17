#region Copyright

// ===============================================================================
//   Project Name        :    PxeServices
//   Project Description :
//   ===============================================================================
//   File Name           :    IDhcpUserStorage.cs
//   File Version        :    v1.0.0.0
//   File Description    :
//   Author              :    wonderlive
//   Create Time         :    2025-10-13 8:55
//   Update Time         :    2025-10-13 8:55
// 
//   ===============================================================================
//      _____  _____  / /_
//     / ___/ / ___/ / __/
//    (__  ) / /__  / /_
//   /____/  \___/  \__/     Copyright © 2025 SCT Tech. Co., Ltd. All rights reserved.
//   ===============================================================================

#endregion

using System.Security.Principal;

namespace PxeServices.Entities;

public interface IEntity<TKey>
{
    TKey Id { get; set; }
}

public abstract class Entity<TKey> : IEntity<TKey>
{
    public virtual TKey Id { get; set; } 
}
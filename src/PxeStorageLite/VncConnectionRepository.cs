#region Copyright

// ===============================================================================
//   Project Name        :    PxeStorageLite
//   Project Description :
//   ===============================================================================
//   File Name           :    VncConnectionRepository.cs
//   File Version        :    v1.0.0.0
//   File Description    :
//   Author              :    wonderlive
//   Create Time         :    2025-10-15 14:24
//   Update Time         :    2025-10-15 14:24
// 
//   ===============================================================================
//      _____  _____  / /_
//     / ___/ / ___/ / __/
//    (__  ) / /__  / /_
//   /____/  \___/  \__/     Copyright © 2025 SCT Tech. Co., Ltd. All rights reserved.
//   ===============================================================================

#endregion

using Microsoft.EntityFrameworkCore;
using PxeServices.Entities.VncClient;

namespace PxeStorageLite;

public class VncConnectionRepository(IDbContextFactory<PxeDbContext> factory) : Repository<Guid, VncConnection>(factory), IVncConnectionRepository
{
}
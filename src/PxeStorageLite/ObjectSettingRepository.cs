#region Copyright

// ===============================================================================
//   Project Name        :    PxeStorageLite
//   Project Description :
//   ===============================================================================
//   File Name           :    ObjectSettingRepository.cs
//   File Version        :    v1.0.0.0
//   File Description    :
//   Author              :    wonderlive
//   Create Time         :    2025-10-16 13:56
//   Update Time         :    2025-10-16 13:56
// 
//   ===============================================================================
//      _____  _____  / /_
//     / ___/ / ___/ / __/
//    (__  ) / /__  / /_
//   /____/  \___/  \__/     Copyright © 2025 SCT Tech. Co., Ltd. All rights reserved.
//   ===============================================================================

#endregion

using Microsoft.EntityFrameworkCore;
using PxeServices.Entities.Settings;
using System.Xml.Linq;

namespace PxeStorageLite;

public class ObjectSettingRepository(IDbContextFactory<PxeDbContext> factory) : Repository<Guid, ObjectSetting>(factory), IObjectSettingRepository
{
    #region Implementation of IObjectSettingRepository

    public T? GetObjectSetting<T>()
    {
        var setting = Queryable.FirstOrDefault(x => x.Name == typeof(T).FullName);
        return setting == null ? default : setting.Get<T>();
    }

    public async Task<T?> GetObjectSettingAsync<T>()
    {
        var setting = await Queryable.FirstOrDefaultAsync(x => x.Name == typeof(T).FullName);
        return setting == null ? default : setting.Get<T>();
    }

    public void SetObjectSetting<T>(T value)
    {
        if (DbContext.ObjectSettings.FirstOrDefault(x => x.Name == typeof(T).FullName) is { } exists)
        {
            exists.Set(value);
            DbContext.Update(exists);
        }
        else
        {
            var setting = new ObjectSetting() { Name = typeof(T).FullName };
            setting.Set(value);
            DbContext.Add(setting);
        }

        DbContext.SaveChanges();
    }

    public async Task SetObjectSettingAsync<T>(T value)
    {
        if (await DbContext.ObjectSettings.FirstOrDefaultAsync(x => x.Name == typeof(T).FullName) is { } exists)
        {
            exists.Set(value);
            DbContext.Update(exists);
        }
        else
        {
            var setting = new ObjectSetting() { Name = typeof(T).FullName };
            setting.Set(value);
            DbContext.Add(setting);
        }

        await DbContext.SaveChangesAsync();
    }

    #endregion
}
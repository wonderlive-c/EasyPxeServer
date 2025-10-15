using Microsoft.EntityFrameworkCore;
using PxeServices.Entities.Dhcp;

namespace PxeStorageLite;

public class DhcpUserRepository(IDbContextFactory<PxeDbContext> factory) : Repository<Guid, DhcpUser>(factory), IDhcpUserRepository
{
    #region Implementation of IDhcpUserRepository

    public async Task<DhcpUser?> GetByMacAddressOrCreate(string mac, Action<DhcpUser>? onCreated = null)
    {
        var entity = await DbContext.DhcpUsers.FirstOrDefaultAsync(u => u.MacAddress == mac);

        if (entity is null
         && onCreated is not null)
        {
            entity = new DhcpUser() { MacAddress = mac };
            onCreated(entity);
            await DbContext.DhcpUsers.AddAsync(entity);
            await DbContext.SaveChangesAsync();
        }

        return entity;
    }

    #endregion
}
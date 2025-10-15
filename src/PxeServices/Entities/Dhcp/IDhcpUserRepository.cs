using PxeStorageLite;

namespace PxeServices.Entities.Dhcp;

public interface IDhcpUserRepository : IRepository<Guid, DhcpUser>
{
    Task<DhcpUser?> GetByMacAddressOrCreate(string mac, Action<DhcpUser>? onCreate=null);
}
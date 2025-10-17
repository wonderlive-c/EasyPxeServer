namespace PxeServices.Entities.Settings;

public interface IObjectSettingRepository : IRepository<Guid, ObjectSetting>
{
    T? GetObjectSetting<T>();

    Task<T?> GetObjectSettingAsync<T>();

    void SetObjectSetting<T>(T value);

    Task SetObjectSettingAsync<T>(T value);
}
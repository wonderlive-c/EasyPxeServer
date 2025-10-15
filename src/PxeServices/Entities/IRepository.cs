namespace PxeServices.Entities;

public interface IRepository<in TKey, T> where T : IEntity<TKey>
{
    public Task<IEnumerable<T>> GetListAsync();
    public Task<T?>             GetAsync(TKey    id);
    public Task<T?>             AddAsync(T       entity);
    public Task<bool>           UpdateAsync(T    entity);
    public Task<bool>           DeleteAsync(TKey id);
    public Task<bool>           ExistsAsync(TKey id);
    Task<bool>                  DeleteAsync(T    entity);
    Task<bool>                  ExistsAsync(T    entity);


    public IEnumerable<T> GetList();
    public T?             Get(TKey    id);
    public T?             Add(T       entity);
    public bool           Update(T    entity);
    public bool           Delete(TKey id);
    public bool           Exists(TKey id);
    bool                  Delete(T    entity);
    bool                  Exists(T    entity);
}
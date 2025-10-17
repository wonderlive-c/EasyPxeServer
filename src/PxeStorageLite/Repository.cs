using Microsoft.EntityFrameworkCore;
using PxeServices.Entities;

namespace PxeStorageLite;

public abstract class Repository<TKey, T>(IDbContextFactory<PxeDbContext> factory) : IRepository<TKey, T> where T : class, IEntity<TKey>
{
    protected PxeDbContext DbContext { get; } = factory.CreateDbContext();

    public IQueryable<T> Queryable => DbContext.Set<T>();

    public async Task<T> AddAsync(T entity)
    {
        DbContext.Add(entity);
        await DbContext.SaveChangesAsync();
        return entity;
    }

    public async Task<bool> DeleteAsync(TKey id)
    {
        var entity = await DbContext.FindAsync<T>(id);
        if (entity == null) return false;
        DbContext.Remove(entity);
        await DbContext.SaveChangesAsync();
        return true;
    }


    public async Task<T?> GetAsync(TKey id) { return await DbContext.Set<T>().FindAsync(id); }

    public async Task<IEnumerable<T>> GetListAsync() { return await DbContext.Set<T>().ToListAsync(); }

    public async Task<bool> UpdateAsync(T entity)
    {
        DbContext.Update(entity);
        await DbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(TKey id) { return await DbContext.Set<T>().AnyAsync(e => e.Id!.Equals(id)); }

    public async Task<bool>     DeleteAsync(T entity) => await DeleteAsync(entity.Id);
    public async Task<bool>     ExistsAsync(T entity) => await ExistsAsync(entity.Id);
    public       IEnumerable<T> GetList()             { return DbContext.Set<T>().ToList(); }

    public T? Get(TKey id) { return DbContext.Set<T>().Find(id); }

    public T? Add(T entity)
    {
        DbContext.Add(entity);
        DbContext.SaveChanges();
        return entity;
    }

    public bool Update(T entity)
    {
        DbContext.Update(entity);
        DbContext.SaveChanges();
        return true;
    }

    public bool Delete(TKey id)
    {
        var entity = DbContext.Find<T>(id);
        if (entity == null) return false;
        DbContext.Remove(entity);
        DbContext.SaveChanges();
        return true;
    }

    public bool Exists(TKey id) { return DbContext.Set<T>().Any(e => e.Id!.Equals(id)); }

    public bool Delete(T entity) { return Delete(entity.Id); }

    public bool Exists(T entity) { return Exists(entity.Id); }
}
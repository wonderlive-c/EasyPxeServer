using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace PxeStorageLite;

public class PxeDbContextFactory : IDesignTimeDbContextFactory<PxeDbContext>
{
    /// <summary>
    /// 创建PxeDbContext实例
    /// </summary>
    /// <param name="args">设计时参数（通常忽略）</param>
    /// <returns>PxeDbContext实例</returns>
    public PxeDbContext CreateDbContext(string[] args)
    {
        // 创建数据库上下文选项
        var optionsBuilder = new DbContextOptionsBuilder<PxeDbContext>();

        // 配置数据库提供程序和连接字符串
        // 根据实际使用的数据库类型选择对应的方法
        optionsBuilder.UseSqlite(b => b.MigrationsAssembly(typeof(PxeDbContext).Assembly.FullName));

        // 可选：配置日志以调试迁移问题
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.LogTo(Console.WriteLine);

        return new PxeDbContext(optionsBuilder.Options);
    }
}
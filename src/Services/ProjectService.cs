using Data;
using Microsoft.EntityFrameworkCore;

namespace Services;

public class ProjectService
{
    public AppDbContext CreateContext(string dbPath)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={dbPath}").Options;
        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}

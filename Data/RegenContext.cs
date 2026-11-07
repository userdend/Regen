using Microsoft.EntityFrameworkCore;

namespace Regen.Data;

public class RegenContext : DbContext
{
    public RegenContext(DbContextOptions<RegenContext> options)
        : base(options)
    {
    }
}
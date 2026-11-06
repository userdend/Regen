using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Regen.Models;

namespace Regen.Data
{
    public class RegenContext : DbContext
    {
        public RegenContext (DbContextOptions<RegenContext> options)
            : base(options)
        {
        }
        public DbSet<Regen.Models.Transactions> Transactions { get; set; } = default!;
    }
}

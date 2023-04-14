using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace StinClasses.Models
{
    public partial class StinDbContext : DbContext
    {
        public IQueryable<VzTree> fn_GetTreeById(string id, bool findRoot) => FromExpression(() => fn_GetTreeById(id, findRoot));
        partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDbFunction(() => fn_GetTreeById(default, default));
        }
    }
}

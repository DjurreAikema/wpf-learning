using Microsoft.EntityFrameworkCore;
using WpfApp1.Shared.Classes;

namespace WpfApp1.Data;

public class SnackDbContext(DbContextOptions<SnackDbContext> options) : DbContext(options)
{
    public DbSet<Snack> Snacks { get; set; }
}
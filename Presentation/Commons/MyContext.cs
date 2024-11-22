using System.Data.Entity.ModelConfiguration;
using System.Reflection;
using Microsoft.EntityFrameworkCore;

namespace Presentation.Commons;

// public class MyContext : DbContext
// {
//     public DbSet<User> User { get; set; }
//
//     public MyContext(DbContextOptions<MyContext> options) : base(options)
//     {
//     }
//
//     protected override void OnModelCreating(ModelBuilder modelBuilder)
//     {
//         modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
//     }
// }
//
// public class EstoqueMap : EntityTypeConfiguration<User>
// {
//     public EstoqueMap()
//     {
//         ToTable("TB_USER")
//             .HasKey(k => k.Id);
//         //
//         // Property(p => p.Id)
//         //     .ValueGeneratedOnAdd();
//     }
// }
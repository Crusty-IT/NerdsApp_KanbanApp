using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using KanbanApp.Backend.Models;

namespace KanbanApp.Backend.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Board> Boards { get; set; }
    public DbSet<Column> Columns { get; set; }
    public DbSet<Card> Cards { get; set; }
    public DbSet<BoardMember> BoardMembers { get; set; }
    public DbSet<ProjectMember> ProjectMembers { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BoardMember>()
            .HasKey(bm => new { bm.UserId, bm.BoardId });

        modelBuilder.Entity<BoardMember>()
            .HasOne(bm => bm.User)
            .WithMany(u => u.BoardMembers)
            .HasForeignKey(bm => bm.UserId);

        modelBuilder.Entity<BoardMember>()
            .HasOne(bm => bm.Board)
            .WithMany(b => b.BoardMembers)
            .HasForeignKey(bm => bm.BoardId);

        modelBuilder.Entity<ProjectMember>()
            .HasKey(pm => new { pm.UserId, pm.ProjectId });

        modelBuilder.Entity<ProjectMember>()
            .HasOne(pm => pm.User)
            .WithMany(u => u.ProjectMembers)
            .HasForeignKey(pm => pm.UserId);

        modelBuilder.Entity<ProjectMember>()
            .HasOne(pm => pm.Project)
            .WithMany(p => p.Members)
            .HasForeignKey(pm => pm.ProjectId);

        modelBuilder.Entity<Project>()
            .HasOne(p => p.Owner)
            .WithMany()
            .HasForeignKey(p => p.OwnerId);

        modelBuilder.Entity<Board>()
            .HasOne(b => b.Project)
            .WithMany(p => p.Boards)
            .HasForeignKey(b => b.ProjectId)
            .IsRequired(false);

        modelBuilder.Entity<RefreshToken>()
            .HasOne(rt => rt.User)
            .WithMany()
            .HasForeignKey(rt => rt.UserId);

        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany()
            .HasForeignKey(n => n.UserId);
    }
}
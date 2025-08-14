using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using soft20181_starter.Services;

namespace soft20181_starter.Models
{
    public class EventAppDbContext : IdentityDbContext<AppUser>
    {
        public EventAppDbContext(DbContextOptions<EventAppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Contact> Contacts { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public new DbSet<AppUser> Users { get; set; }
        public DbSet<TheEvent> Events { get; set; }
        public DbSet<EventAttendance> EventAttendances { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure AuditLog entity
            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasIndex(e => e.EntityName);
                entity.HasIndex(e => e.EntityId);
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.Action);
            });

            modelBuilder.Entity<TheEvent>(entity =>
            {
                entity.Property(e => e.images)
                    .HasConversion(
                        v => string.Join(';', v),
                        v => v.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList(),
                        new ValueComparer<List<string>>(
                            (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                            c => c != null ? c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())) : 0,
                            c => c != null ? c.ToList() : new List<string>()
                        )
                    );

                entity.HasIndex(e => e.location);
                entity.HasIndex(e => e.date);
                entity.HasIndex(e => e.price);
            });
            
            modelBuilder.Entity<AppUser>(entity =>
            {
                entity.HasIndex(e => e.Email).IsUnique();
            });
            
            // Configure Event Attendance
            modelBuilder.Entity<EventAttendance>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.Event)
                    .WithMany()
                    .HasForeignKey(e => e.EventId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasIndex(e => new { e.UserId, e.EventId }).IsUnique();
            });

            // Configure PaymentTransaction
            modelBuilder.Entity<PaymentTransaction>(entity =>
            {
                entity.HasOne(pt => pt.User)
                    .WithMany()
                    .HasForeignKey(pt => pt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }


    }
}


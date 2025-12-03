using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AttendanceManagementSystem.Models;

namespace AttendanceManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        
        public DbSet<Batch> Batches { get; set; }
        public DbSet<Section> Sections { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<TimeTable> TimeTables { get; set; }
        public DbSet<Attendance> Attendances { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure relationships
            builder.Entity<Section>()
                .HasOne(s => s.Batch)
                .WithMany(b => b.Sections)
                .HasForeignKey(s => s.BatchId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Student>()
                .HasOne(s => s.Section)
                .WithMany(sec => sec.Students)
                .HasForeignKey(s => s.SectionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TimeTable>()
                .HasOne(tt => tt.Course)
                .WithMany(c => c.TimeTables)
                .HasForeignKey(tt => tt.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TimeTable>()
                .HasOne(tt => tt.Section)
                .WithMany()
                .HasForeignKey(tt => tt.SectionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TimeTable>()
                .HasOne(tt => tt.Teacher)
                .WithMany(t => t.TimeTables)
                .HasForeignKey(tt => tt.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Attendance>()
                .HasOne(a => a.Student)
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Attendance>()
                .HasOne(a => a.Course)
                .WithMany(c => c.Attendances)
                .HasForeignKey(a => a.CourseId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Attendance>()
                .HasOne(a => a.Section)
                .WithMany()
                .HasForeignKey(a => a.SectionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Attendance>()
                .HasOne(a => a.Teacher)
                .WithMany()
                .HasForeignKey(a => a.TeacherId)
                .OnDelete(DeleteBehavior.SetNull);

            // Add unique constraints
            builder.Entity<Student>()
                .HasIndex(s => s.RollNumber)
                .IsUnique();

            builder.Entity<Course>()
                .HasIndex(c => c.CourseCode)
                .IsUnique();

            // Add composite unique constraint for attendance
            builder.Entity<Attendance>()
                .HasIndex(a => new { a.StudentId, a.CourseId, a.Date })
                .IsUnique();
        }
    }
}

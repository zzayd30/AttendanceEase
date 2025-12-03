using System.ComponentModel.DataAnnotations;

namespace AttendanceManagementSystem.Models
{
    public class Attendance
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }

        [Required]
        [Display(Name = "Status")]
        public AttendanceStatus Status { get; set; }

        [Display(Name = "Remarks")]
        public string? Remarks { get; set; }

        [Display(Name = "Marked At")]
        public DateTime MarkedAt { get; set; } = DateTime.Now;

        // Foreign Keys
        [Required]
        public int StudentId { get; set; }
        public Student? Student { get; set; }

        [Required]
        public int CourseId { get; set; }
        public Course? Course { get; set; }

        [Required]
        public int SectionId { get; set; }
        public Section? Section { get; set; }

        public int? TeacherId { get; set; }
        public Teacher? Teacher { get; set; }
    }

    public enum AttendanceStatus
    {
        Present,
        Absent,
        Late,
        Excused
    }
}

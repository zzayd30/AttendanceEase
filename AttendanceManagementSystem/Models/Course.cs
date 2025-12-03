using System.ComponentModel.DataAnnotations;

namespace AttendanceManagementSystem.Models
{
    public class Course
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Course Code")]
        public string CourseCode { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Course Name")]
        public string CourseName { get; set; } = string.Empty;

        [Display(Name = "Credit Hours")]
        [Range(1, 6)]
        public int CreditHours { get; set; }

        [Display(Name = "Description")]
        public string? Description { get; set; }

        public ICollection<TimeTable>? TimeTables { get; set; }
        public ICollection<Attendance>? Attendances { get; set; }
    }
}

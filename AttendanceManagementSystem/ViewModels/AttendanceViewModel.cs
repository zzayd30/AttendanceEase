using AttendanceManagementSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace AttendanceManagementSystem.ViewModels
{
    public class AttendanceViewModel
    {
        public int CourseId { get; set; }
        public string? CourseName { get; set; }
        public int SectionId { get; set; }
        public string? SectionName { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;
        
        public List<StudentAttendanceItem> Students { get; set; } = new();
    }

    public class StudentAttendanceItem
    {
        public int StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? RollNumber { get; set; }
        public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
        public string? Remarks { get; set; }
    }
}

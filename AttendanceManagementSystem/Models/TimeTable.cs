using System.ComponentModel.DataAnnotations;

namespace AttendanceManagementSystem.Models
{
    public class TimeTable
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Day of Week")]
        public DayOfWeek DayOfWeek { get; set; }

        [Required]
        [Display(Name = "Start Time")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required]
        [Display(Name = "End Time")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [Display(Name = "Room Number")]
        public string? RoomNumber { get; set; }

        // Foreign Keys
        [Required]
        public int CourseId { get; set; }
        public Course? Course { get; set; }

        [Required]
        public int SectionId { get; set; }
        public Section? Section { get; set; }

        [Required]
        public int TeacherId { get; set; }
        public Teacher? Teacher { get; set; }
    }
}

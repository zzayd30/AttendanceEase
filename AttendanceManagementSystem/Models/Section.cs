using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AttendanceManagementSystem.Models
{
    public class Section
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty; // e.g. "Section A"

        public int BatchId { get; set; }
        public Batch? Batch { get; set; }

        public ICollection<Student>? Students { get; set; }
    }
}

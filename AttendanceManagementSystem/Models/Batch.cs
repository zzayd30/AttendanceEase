using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using static System.Collections.Specialized.BitVector32;

namespace AttendanceManagementSystem.Models
{
    public class Batch
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;  // e.g. "Batch 2022"

        public ICollection<Section>? Sections { get; set; }
    }
}

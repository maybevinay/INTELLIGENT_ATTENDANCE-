using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IntelligentAttendanceSystem.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = "Student"; // "Admin" or "Student"

        // For student face recognition mapping (e.g. unique Face API ID or local identifier)
        public string FaceIdentifier { get; set; } = string.Empty;

        public string Department { get; set; } = string.Empty;

        public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();
    }
}

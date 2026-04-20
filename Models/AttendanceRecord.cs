using System;
using System.ComponentModel.DataAnnotations;

namespace IntelligentAttendanceSystem.Models
{
    public class AttendanceRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public User? User { get; set; }

        [Required]
        public DateTime Timestamp { get; set; }

        public string? Status { get; set; } // "Present", "Late", "Absent"
        
        // Optional: Save the image path or base64 locally for audit
        public string? CapturedImagePath { get; set; }
    }
}


using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [StringLength(15)]
        public string? Phone { get; set; }

        public string? Address { get; set; }
        public string? Ward { get; set; }
        public string? District { get; set; }
        public string? City { get; set; }

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "Customer"; // Mặc định là Customer

        public bool IsLocked { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
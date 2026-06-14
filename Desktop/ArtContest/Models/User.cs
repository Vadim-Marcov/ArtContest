using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArtContest.Models
{
    [Table("User")]
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Login { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string BirthDate { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        public string? ProfilePhoto { get; set; }

        [Required]
        [ForeignKey("Role")]
        public int IdRole { get; set; }
        public Role? Role { get; set; }

        [Required]
        [ForeignKey("Region")]
        public int IdRegion { get; set; }
        public Region? Region { get; set; }
    }
}

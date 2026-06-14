using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArtContest.Models
{
    [Table("Moderator_Log")]
    public class ModeratorLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Status { get; set; } = string.Empty;

        public string? ModComment { get; set; }

        [Required]
        [ForeignKey("User")]
        public int IdUser { get; set; }
        public User? User { get; set; }

        [Required]
        public string ResponseDate { get; set; } = string.Empty;
    }
}

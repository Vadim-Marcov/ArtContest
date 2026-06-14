using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArtContest.Models
{
    [Table("Stage")]
    public class Stage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string StageName { get; set; } = string.Empty;
    }
}

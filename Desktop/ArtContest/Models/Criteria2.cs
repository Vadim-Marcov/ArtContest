using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArtContest.Models
{
    [Table("Criteria_2")]
    public class Criteria2
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Criteria2Name { get; set; } = string.Empty;
    }
}

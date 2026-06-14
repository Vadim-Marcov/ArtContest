using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArtContest.Models
{
    [Table("Criteria_1")]
    public class Criteria1
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Criteria1Name { get; set; } = string.Empty;
    }
}

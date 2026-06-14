using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArtContest.Models
{
    [Table("Judging_Period")]
    public class JudgingPeriod
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string JudStartDate { get; set; } = string.Empty;

        [Required]
        public string JudEndDate { get; set; } = string.Empty;
    }
}

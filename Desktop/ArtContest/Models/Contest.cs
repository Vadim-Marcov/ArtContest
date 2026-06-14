using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArtContest.Models
{
    [Table("Contest")]
    public class Contest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Rules { get; set; } = string.Empty;

        [Required]
        public string ContestImage { get; set; } = string.Empty;

        [Required]
        [ForeignKey("ContestCategory")]
        public int IdCategory { get; set; }
        public ContestCategory? Category { get; set; }

        [Required]
        [ForeignKey("Stage")]
        public int IdStage { get; set; }
        public Stage? Stage { get; set; }

        [Required]
        [ForeignKey("ApplicationPeriod")]
        public int IdAppPeriod { get; set; }
        public ApplicationPeriod? ApplicationPeriod { get; set; }

        [Required]
        [ForeignKey("JudgingPeriod")]
        public int IdJudPeriod { get; set; }
        public JudgingPeriod? JudgingPeriod { get; set; }

        [Required]
        [ForeignKey("Criteria1")]
        public int IdCriteria1 { get; set; }
        public Criteria1? Criteria1 { get; set; }

        [Required]
        [ForeignKey("Criteria2")]
        public int IdCriteria2 { get; set; }
        public Criteria2? Criteria2 { get; set; }
    }
}

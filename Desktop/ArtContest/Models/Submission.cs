using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArtContest.Models
{
    [Table("Submission")]
    public class Submission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string SubmissionImage { get; set; } = string.Empty;

        [Required]
        public string SubmissionDate { get; set; } = string.Empty;

        public string? AuthorDescription { get; set; }

        public double? TotalScore { get; set; }

        [Required]
        [ForeignKey("User")]
        public int IdUser { get; set; }
        public User? User { get; set; }

        [Required]
        [ForeignKey("Contest")]
        public int IdContest { get; set; }
        public Contest? Contest { get; set; }

        [ForeignKey("ModeratorLog")]
        public int? IdModLog { get; set; }
        public ModeratorLog? ModeratorLog { get; set; }

        public List<JuryAssessment> JuryAssessments { get; set; } = new();
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArtContest.Models
{
    [Table("Jury_Assessment")]
    public class JuryAssessment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int Score1 { get; set; }

        [Required]
        public int Score2 { get; set; }

        public string? JuryComment { get; set; }

        [Required]
        [ForeignKey("User")]
        public int IdUser { get; set; }
        public User? User { get; set; }

        [Required]
        [ForeignKey("Submission")]
        public int IdSubmission { get; set; }
        public Submission? Submission { get; set; }
    }
}

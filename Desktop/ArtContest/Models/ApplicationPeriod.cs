using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArtContest.Models
{
    [Table("Application_Period")]
    public class ApplicationPeriod
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string AppStartDate { get; set; } = string.Empty;

        [Required]
        public string AppEndDate { get; set; } = string.Empty;
    }
}

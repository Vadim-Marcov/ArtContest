using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArtContest.Models
{
    [Table("Region")]
    public class Region
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string RegionName { get; set; } = string.Empty;
    }
}

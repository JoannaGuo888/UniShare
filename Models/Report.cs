using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UniShare.Models
{
    public class Report
    {
        [Key]
        public int ReportId { get; set; }
        [Required]
        public int ReporterUserId { get; set; }
        [Required]
        public int SubjectUserId { get; set; }
        [Required]
        public int RelatedRideId { get; set; }
        public string ReportReason { get; set; }
        [RegularExpression(@"^(Normal|High|Low)$")]

        public string Priority { get; set; }
        [RegularExpression(@"^(Open|Resolved)$")]

        public string ReportStatus { get; set; }
        public DateTime CreatedAt { get; set; }



    }
}

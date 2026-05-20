using System.ComponentModel.DataAnnotations;

namespace UniShare.Models
{
    public class SystemAuditLog
    {
        [Key]
        public int LogId { get; set; }
        [Required]
        public int AdminUserId { get; set; }
        public string ActionTaken { get; set; }
        public string AffectedEntity { get; set; }
        public DateTime ActionTime { get; set; } = DateTime.Now;
    }
}

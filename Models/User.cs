using System.ComponentModel.DataAnnotations;

namespace UniShare.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }
        [Required]
        [StringLength(50, MinimumLength =3)]
        public string UserName { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string PasswordHash { get; set; }
        [Required]
        [RegularExpression(@"^(Driver|Passenger|Admin)$")]
        public string Role { get; set; }
        [Phone]
        public string PhoneNumber { get; set; }
        public string HomeAddress { get; set; }
        [RegularExpression(@"^(Active|Suspended|Banned)$")]

        public string AccountStatus { get; set; }
        public DateTime JoinDate { get; set; }

    }
}

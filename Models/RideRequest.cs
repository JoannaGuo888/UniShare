using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace UniShare.Models
{
    public class RideRequest
    {
        [Key]
        public int RequestId { get; set; }
        [Required]
        public int RideId { get; set; }
        [Required]
        public int PassengerId { get; set; }
        [Required]
        public int DriverId { get; set; }
        public string Message { get; set; }
        public DateTime RequestCreatedTime { get; set; }
        [RegularExpression(@"^(New|Accepted|Declined|CancelledByPassenger|CancelledByDriver|CancelledByAdmin|RideExpired)$")]

        public string RequestStatus { get; set; }
        
        // Navigation
        public Ride Ride { get; set; }
        public User Passenger { get; set; }
        public User Driver { get; set; }
    }
}

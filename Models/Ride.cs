using System.ComponentModel.DataAnnotations;

namespace UniShare.Models
{
    public class Ride
    {
        [Key]
        public int RideId { get; set; }
        [Required]
        public int DriverId { get; set; }
        [Required]
        public string StartLocation { get; set; }
        [Required]
        public string Destination { get; set; }
        [Required]
        public DateTime RideDate { get; set; }
        public TimeSpan RideTime { get; set; }
        [Required]
        public int AvailableSeats { get; set; }
        public double CostPerSeat { get; set; }
        [RegularExpression(@"^(Active|Upcoming|Completed|Cancelled|Expired|Disputed|DisputeResolved)$")]

        public string RideStatus { get; set; }

        // Navigation 
        public User Driver { get; set; }

    }
}

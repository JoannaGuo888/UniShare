using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UniShare.Models;

namespace UniShare.Tests
{
    [TestClass]
    public sealed class ModelCheckTests
    {
        [TestMethod]
        public void CreateValidUserObject()
        {
            User User1 = new User
            {
                UserName = "Alice",
                Email = "Alice@TUDublin.ie",
                PasswordHash = "Alice123",
                Role = "Passenger",
                PhoneNumber = "7894561230",
                HomeAddress = "Talbot St"
            };
            Assert.IsNotNull(User1);
        }

        [TestMethod]
        public void ValidateUserInfo()
        {
            User User2 = new User
            {
                UserName = "",  // cannot be null
                Email = "Bob@TUDublin.ie",
                PasswordHash = "Bob123",
                Role = "Driver",
                PhoneNumber = "7894561230",
                HomeAddress = "O'Connell St"
            };

            List<ValidationResult> errors = new List<ValidationResult>();
            ValidationContext Context = new ValidationContext(User2);
            bool validationState = Validator.TryValidateObject(User2, Context, errors, true);

            Assert.IsFalse(validationState);
            Assert.AreNotEqual(0, errors.Count);
        }

        [TestMethod]
        public void ValidateUserInfo2()
        {
            User User3 = new User
            {
                UserName = "Carrie",
                Email = "Carrie@TUDublin.ie",
                PasswordHash = "Carrie123",
                Role = "Driver",
                PhoneNumber = "4567893210",
                HomeAddress = "West Campus Area",
                AccountStatus = "Active"
            };

            List<ValidationResult> errors = new List<ValidationResult>();
            ValidationContext Context = new ValidationContext(User3);
            bool validationState = Validator.TryValidateObject(User3, Context, errors, true);

            Assert.IsTrue(validationState);
            Assert.AreEqual(0, errors.Count);
        }

        [TestMethod]
        public void ValidateUserInfo3()
        {
            User User4 = new User
            {
                UserName = "Darrie",
                Email = "wrongmailaddress",  // not valid email format
                PasswordHash = "Darrie123",
                Role = "Passenger",
                PhoneNumber = "4567893210",
                HomeAddress = "West Campus Area"
            };

            List<ValidationResult> errors = new List<ValidationResult>();
            ValidationContext Context = new ValidationContext(User4);
            bool validationState = Validator.TryValidateObject(User4, Context, errors, true);

            Assert.IsFalse(validationState);
            Assert.AreEqual(1, errors.Count);
        }

        [TestMethod]
        public void CreateValidRideObject()
        {
            Ride Ride1 = new Ride
            {
                DriverId = 5,
                StartLocation = "Main School Entrance",
                Destination = "Town Center",
                RideDate = DateTime.Now.AddDays(2),
                RideTime = new TimeSpan(16, 20, 0),
                AvailableSeats = 4,
                CostPerSeat = 6.50,
                RideStatus = "Upcoming"
            };
            Assert.IsNotNull(Ride1);
        }

        [TestMethod]
        public void CreateInvalidRide2()
        {
            Ride Ride2 = new Ride
            {
                DriverId = 5,
                StartLocation = null,  // cannot be null
                Destination = "Town Center",
                RideDate = DateTime.Now.AddDays(2),
                RideTime = new TimeSpan(16, 20, 0),
                AvailableSeats = 4,
                CostPerSeat = 6.50
            };

            List<ValidationResult> errors = new List<ValidationResult>();
            ValidationContext Context = new ValidationContext(Ride2);
            bool validationState = Validator.TryValidateObject(Ride2, Context, errors, true);

            Assert.IsFalse(validationState);
            Assert.AreEqual(1, errors.Count);
        }

        [TestMethod]
        public void CreateInvalidRide3()
        {
            Ride Ride3 = new Ride
            {
                DriverId = 5,
                StartLocation = "Main School Entrance",
                Destination = "Town Center",
                RideDate = DateTime.Now.AddDays(2),
                AvailableSeats = -3,  // cannot be negative
                CostPerSeat = 6.50
            };

            List<ValidationResult> errors = new List<ValidationResult>();
            ValidationContext Context = new ValidationContext(Ride3);
            bool validationState = Validator.TryValidateObject(Ride3, Context, errors, true);

            Assert.IsFalse(validationState);
            Assert.AreEqual(1, errors.Count);
        }

        [TestMethod]
        public void CreateInvalidRide4()
        {
            Ride Ride4 = new Ride
            {
                DriverId = 5,
                StartLocation = "Main School Entrance",
                Destination = "Town Center",
                RideDate = DateTime.Now.AddDays(2),
                AvailableSeats = 3,
                CostPerSeat = -3  // cannot be negative
            };

            List<ValidationResult> errors = new List<ValidationResult>();
            ValidationContext Context = new ValidationContext(Ride4);
            bool validationState = Validator.TryValidateObject(Ride4, Context, errors, true);

            Assert.IsFalse(validationState);
            Assert.AreEqual(1, errors.Count);
        }

        [TestMethod]
        public void CreateValidRequest()
        {
            RideRequest request = new RideRequest
            {
                RideId = 8,
                DriverId = 5,
                PassengerId = 12,
                RequestCreatedTime = DateTime.Now,
                RequestStatus = "New"   
            };

            List<ValidationResult> errors = new List<ValidationResult>();
            ValidationContext Context = new ValidationContext(request);
            bool validationState = Validator.TryValidateObject(request, Context, errors, true);

            Assert.IsTrue(validationState);
            Assert.AreEqual(0, errors.Count);
        }


        [TestMethod]
        public void CreateInValidRequest()
        {
            RideRequest request1 = new RideRequest
            {
                RideId = 8,
                DriverId = 5,
                PassengerId = 12,
                RequestCreatedTime = DateTime.Now,
                RequestStatus = "Pending"   // "Pending" not valid
            };

            List<ValidationResult> errors = new List<ValidationResult>();
            ValidationContext Context = new ValidationContext(request1);
            bool validationState = Validator.TryValidateObject(request1, Context, errors, true);

            Assert.IsFalse(validationState);
            Assert.AreEqual(1, errors.Count);
        }

        [TestMethod]
        public void CreateInValidRequest2()
        {
            RideRequest request2 = new RideRequest
            {
                RideId = 8,
                DriverId = 5,
                PassengerId = 12,
                RequestCreatedTime = DateTime.Now,
                RequestStatus = " "  // cannot be empty
            };

            List<ValidationResult> errors = new List<ValidationResult>();
            ValidationContext Context = new ValidationContext(request2);
            bool validationState = Validator.TryValidateObject(request2, Context, errors, true);

            Assert.IsFalse(validationState);
            Assert.AreEqual(1, errors.Count);
        }
    }
}
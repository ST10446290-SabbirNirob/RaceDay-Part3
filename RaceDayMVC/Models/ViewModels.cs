using System.ComponentModel.DataAnnotations;

namespace RaceDayMVC.Models;

public sealed class LoginForm
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";
}
public sealed class RegisterForm
{
    [Required, StringLength(50)] public string FirstName { get; set; } = "";
    [Required, StringLength(50)] public string LastName { get; set; } = "";
    [Required, EmailAddress, StringLength(100)] public string Email { get; set; } = "";
    [Required, MinLength(8)] public string Password { get; set; } = "";
    [Required] public string Role { get; set; } = "Participant";
}
public sealed class AuthUser
{
    public int UserID { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Role { get; set; } = "";
    public string Email { get; set; } = "";
}
public sealed class RaceEvent
{
    public int EventID { get; set; }
    public int OrganiserID { get; set; }
    public string EventName { get; set; } = "";
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public DateTime RegistrationDeadline { get; set; }
    public string Location { get; set; } = "";
    public string EventType { get; set; } = "";
    public decimal? DistanceKm { get; set; }
    public string? BannerUrl { get; set; }
    public int EnrolmentCount { get; set; } // Calculated in MVC from API enrolment data.
}
public sealed class EventForm
{
    public int EventID { get; set; }
    [Required, StringLength(100)] public string EventName { get; set; } = "";
    [StringLength(500)] public string? Description { get; set; }
    [Required] public DateTime EventDate { get; set; } = DateTime.Today.AddDays(30);
    [Required] public DateTime RegistrationDeadline { get; set; } = DateTime.Today.AddDays(20);
    [Required, StringLength(150)] public string Location { get; set; } = "";
    [Required] public string EventType { get; set; } = "Running";
    [Range(typeof(decimal), "0.01", "9999.99")] public decimal? DistanceKm { get; set; }
    public string? BannerUrl { get; set; }
}
public sealed class RaceCategory
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string CategoryName { get; set; } = "";
    public decimal DistanceKm { get; set; }
    public decimal EntryFee { get; set; }
    public int MaxParticipants { get; set; }
}
public sealed class CategoryForm
{
    public int EventID { get; set; }
    [Required, StringLength(100)] public string CategoryName { get; set; } = "";
    [Range(typeof(decimal), "0.01", "9999.99")] public decimal DistanceKm { get; set; }
    [Range(0, 999999)] public decimal EntryFee { get; set; }
    [Range(1, 1000000)] public int MaxParticipants { get; set; } = 100;
}
public sealed class EventDetail
{
    public RaceEvent Event { get; set; } = new();
    public List<RaceCategory> Categories { get; set; } = new();
}
public sealed class RaceEnrolment
{
    public int EnrolmentID { get; set; }
    public int EventID { get; set; }
    public int ParticipantID { get; set; }
    public int CategoryID { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string RaceNumber { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime EnrolmentDate { get; set; }
}
public sealed class RaceResult
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public int EventID { get; set; }
    public string EventName { get; set; } = "";
    public DateTime EventDate { get; set; }
    public string CategoryName { get; set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public TimeSpan FinishTime { get; set; }
    public int? OverallPosition { get; set; }
    public int? CategoryPosition { get; set; }
    public int TotalFinishers { get; set; }
}
public sealed class ResultForm
{
    [Range(1, int.MaxValue)] public int EnrolmentID { get; set; }
    [Required] public TimeSpan FinishTime { get; set; }
    [Range(1, int.MaxValue)] public int? OverallPosition { get; set; }
    [Range(1, int.MaxValue)] public int? CategoryPosition { get; set; }
}
public sealed class RaceProfile
{
    public int UserID { get; set; }
    [Required, StringLength(50)] public string FirstName { get; set; } = "";
    [Required, StringLength(50)] public string LastName { get; set; } = "";
    [Required, EmailAddress, StringLength(100)] public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public string? ProfilePictureUrl { get; set; }
}
public sealed class OrganiserDashboard
{
    public List<RaceEvent> Events { get; set; } = new();
    public int TotalEnrolments => Events.Sum(e => e.EnrolmentCount);
    public int UpcomingEvents => Events.Count(e => e.EventDate >= DateTime.Today);
}

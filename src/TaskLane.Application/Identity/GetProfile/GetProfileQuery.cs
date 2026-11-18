namespace TaskLane.Application.Identity.GetProfile;

/// <summary>Profile of the signed-in person, identified by the session's user id.</summary>
public sealed record GetProfileQuery(Guid UserId);

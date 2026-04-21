namespace Shared.Common.Exceptions;

public class EventNotFoundException : Exception
{
    public EventNotFoundException(string eventId)
        : base($"Event with ID '{eventId}' was not found.") { }
}

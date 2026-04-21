namespace Shared.Common.Exceptions;

public class TicketAlreadyReservedException : Exception
{
    public TicketAlreadyReservedException(string eventId, string ticketType)
        : base($"Ticket '{ticketType}' for event '{eventId}' is already reserved by another user.") { }
}

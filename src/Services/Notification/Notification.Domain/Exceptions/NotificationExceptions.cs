namespace Notification.Domain.Exceptions;

public class NotificationDomainException : Exception
{
    public NotificationDomainException(string message) : base(message) { }
    
    public NotificationDomainException(string message, Exception innerException) 
        : base(message, innerException) { }
}

public class InvalidNotificationStateException : NotificationDomainException
{
    public InvalidNotificationStateException(string message) : base(message) { }
}

public class TemplateNotFoundException : NotificationDomainException
{
    public TemplateNotFoundException(Guid templateId) 
        : base($"Template with ID {templateId} not found") { }
}

public class TemplateRenderException : NotificationDomainException
{
    public TemplateRenderException(string message, Exception innerException) 
        : base(message, innerException) { }
}

public class InvalidRecipientException : NotificationDomainException
{
    public InvalidRecipientException(string message) : base(message) { }
}

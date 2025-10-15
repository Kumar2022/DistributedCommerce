using Notification.Domain.Events;
using Notification.Domain.ValueObjects;

namespace Notification.Domain.Aggregates;

/// <summary>
/// Notification Template Aggregate Root
/// Represents a reusable template for notifications
/// </summary>
public class NotificationTemplate : AggregateRoot<Guid>
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public NotificationChannel Channel { get; private set; }
    
    public string SubjectTemplate { get; private set; }
    public string BodyTemplate { get; private set; }
    
    public bool IsActive { get; private set; }
    public string? Category { get; private set; }
    
    public Dictionary<string, string> DefaultVariables { get; private set; }
    public Dictionary<string, string> Metadata { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string CreatedBy { get; private set; }
    public string? UpdatedBy { get; private set; }

    private NotificationTemplate() 
    {
        Name = string.Empty;
        Description = string.Empty;
        SubjectTemplate = string.Empty;
        BodyTemplate = string.Empty;
        CreatedBy = string.Empty;
        DefaultVariables = new Dictionary<string, string>();
        Metadata = new Dictionary<string, string>();
    }

    private NotificationTemplate(
        Guid id,
        string name,
        string description,
        NotificationChannel channel,
        string subjectTemplate,
        string bodyTemplate,
        string createdBy,
        string? category = null,
        Dictionary<string, string>? defaultVariables = null,
        Dictionary<string, string>? metadata = null)
    {
        Id = id;
        Name = name;
        Description = description;
        Channel = channel;
        SubjectTemplate = subjectTemplate;
        BodyTemplate = bodyTemplate;
        CreatedBy = createdBy;
        Category = category;
        DefaultVariables = defaultVariables ?? new Dictionary<string, string>();
        Metadata = metadata ?? new Dictionary<string, string>();
        
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new TemplateCreatedEvent(
            Id,
            name,
            channel,
            CreatedAt
        ));
    }

    public static NotificationTemplate Create(
        string name,
        string description,
        NotificationChannel channel,
        string subjectTemplate,
        string bodyTemplate,
        string createdBy,
        string? category = null,
        Dictionary<string, string>? defaultVariables = null,
        Dictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Template name is required", nameof(name));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required", nameof(description));

        if (string.IsNullOrWhiteSpace(subjectTemplate))
            throw new ArgumentException("Subject template is required", nameof(subjectTemplate));

        if (string.IsNullOrWhiteSpace(bodyTemplate))
            throw new ArgumentException("Body template is required", nameof(bodyTemplate));

        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("Created by is required", nameof(createdBy));

        return new NotificationTemplate(
            Guid.NewGuid(),
            name,
            description,
            channel,
            subjectTemplate,
            bodyTemplate,
            createdBy,
            category,
            defaultVariables,
            metadata
        );
    }

    public void Update(
        string name,
        string description,
        string subjectTemplate,
        string bodyTemplate,
        string updatedBy,
        string? category = null,
        Dictionary<string, string>? defaultVariables = null,
        Dictionary<string, string>? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Template name is required", nameof(name));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description is required", nameof(description));

        if (string.IsNullOrWhiteSpace(subjectTemplate))
            throw new ArgumentException("Subject template is required", nameof(subjectTemplate));

        if (string.IsNullOrWhiteSpace(bodyTemplate))
            throw new ArgumentException("Body template is required", nameof(bodyTemplate));

        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("Updated by is required", nameof(updatedBy));

        Name = name;
        Description = description;
        SubjectTemplate = subjectTemplate;
        BodyTemplate = bodyTemplate;
        Category = category;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;

        if (defaultVariables != null)
            DefaultVariables = defaultVariables;

        if (metadata != null)
            Metadata = metadata;

        AddDomainEvent(new TemplateUpdatedEvent(
            Id,
            name,
            UpdatedAt
        ));
    }

    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new TemplateStatusChangedEvent(
            Id,
            IsActive,
            UpdatedAt
        ));
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;

        AddDomainEvent(new TemplateStatusChangedEvent(
            Id,
            IsActive,
            UpdatedAt
        ));
    }

    public NotificationContent RenderContent(Dictionary<string, string>? variables = null)
    {
        var allVariables = new Dictionary<string, string>(DefaultVariables);
        if (variables != null)
        {
            foreach (var (key, value) in variables)
            {
                allVariables[key] = value;
            }
        }

        // Simple template rendering (replace {{variable}} with values)
        var renderedSubject = RenderTemplate(SubjectTemplate, allVariables);
        var renderedBody = RenderTemplate(BodyTemplate, allVariables);

        return NotificationContent.Create(renderedSubject, renderedBody, allVariables);
    }

    private static string RenderTemplate(string template, Dictionary<string, string> variables)
    {
        var result = template;
        foreach (var (key, value) in variables)
        {
            result = result.Replace($"{{{{{key}}}}}", value);
        }
        return result;
    }
}

namespace TaskLane.Domain.Tasks;

/// <summary>
/// Something a person wants to get done. Named <c>TaskItem</c> to avoid clashing with
/// <see cref="System.Threading.Tasks.Task"/>. Created only through <see cref="Create"/>, which enforces the invariants.
/// </summary>
public sealed class TaskItem
{
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    public static readonly DateOnly MinDueDate = new(2000, 1, 1);
    public static readonly DateOnly MaxDueDate = new(2099, 12, 31);

    // Required by EF Core to materialize entities.
    private TaskItem()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Id of the owning account; set on creation and never changed.</summary>
    public Guid OwnerId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public TaskItemStatus Status { get; private set; }

    public DateOnly? DueDate { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public bool IsDeleted => DeletedAt is not null;

    public static TaskItem Create(
        Guid id,
        Guid ownerId,
        string title,
        string? description,
        TaskItemStatus? status,
        DateOnly? dueDate,
        DateTimeOffset now)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("The task id must not be empty.", nameof(id));
        }

        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("The owner id must not be empty.", nameof(ownerId));
        }

        EnsureUtc(now, nameof(now));

        var task = new TaskItem
        {
            Id = id,
            OwnerId = ownerId,
            CreatedAt = now,
            UpdatedAt = now,
        };
        task.SetValues(title, description, status ?? TaskItemStatus.ToDo, dueDate);

        return task;
    }

    /// <summary>Replaces the four editable values. A deleted task must be restored first.</summary>
    public void Update(string title, string? description, TaskItemStatus status, DateOnly? dueDate, DateTimeOffset now)
    {
        if (IsDeleted)
        {
            throw new InvalidOperationException("A deleted task cannot be edited; restore it first.");
        }

        EnsureUtc(now, nameof(now));
        SetValues(title, description, status, dueDate);
        UpdatedAt = now;
    }

    /// <summary>Moves the task to the deleted state. Does nothing if it is already deleted.</summary>
    public void Delete(DateTimeOffset now)
    {
        EnsureUtc(now, nameof(now));

        DeletedAt ??= now;
    }

    /// <summary>Returns the task to the active state with its values unchanged. Does nothing if it is active.</summary>
    public void Restore() => DeletedAt = null;

    private void SetValues(string title, string? description, TaskItemStatus status, DateOnly? dueDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length > TitleMaxLength)
        {
            throw new ArgumentException($"The title must be at most {TitleMaxLength} characters.", nameof(title));
        }

        var normalizedDescription = string.IsNullOrWhiteSpace(description) ? null : description;
        if (normalizedDescription?.Length > DescriptionMaxLength)
        {
            throw new ArgumentException(
                $"The description must be at most {DescriptionMaxLength} characters.", nameof(description));
        }

        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status), status, "The status is not defined.");
        }

        if (dueDate is { } date && (date < MinDueDate || date > MaxDueDate))
        {
            throw new ArgumentOutOfRangeException(
                nameof(dueDate), dueDate, $"The due date must be between {MinDueDate:yyyy-MM-dd} and {MaxDueDate:yyyy-MM-dd}.");
        }

        Title = trimmedTitle;
        Description = normalizedDescription;
        Status = status;
        DueDate = dueDate;
    }

    private static void EnsureUtc(DateTimeOffset value, string parameterName)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("The time must be expressed in UTC.", parameterName);
        }
    }
}

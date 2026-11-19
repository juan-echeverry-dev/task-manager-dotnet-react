namespace TaskLane.Domain.Tasks;

/// <summary>Progress of a task. Any status can change to any other.</summary>
public enum TaskItemStatus
{
    ToDo,
    InProgress,
    Done,
}

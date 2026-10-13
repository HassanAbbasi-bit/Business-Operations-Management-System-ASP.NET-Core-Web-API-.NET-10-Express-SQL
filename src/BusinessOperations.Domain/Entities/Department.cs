using BusinessOperations.Domain.Common;
using BusinessOperations.Domain.Exceptions;

namespace BusinessOperations.Domain.Entities;

/// <summary>
/// Organizational department grouping employees.
/// </summary>
public class Department : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? ManagerEmployeeId { get; private set; }

    public virtual ICollection<Employee> Employees { get; private set; } = new List<Employee>();

    // Parameterless constructor for EF Core
    protected Department() { }

    public Department(string name, string? description = null, Guid? managerEmployeeId = null)
    {
        SetName(name);
        Description = description?.Trim();
        ManagerEmployeeId = managerEmployeeId;
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Department name cannot be empty.");

        Name = name.Trim();
        MarkUpdated();
    }

    public void UpdateDetails(string name, string? description)
    {
        SetName(name);
        Description = description?.Trim();
        MarkUpdated();
    }

    public void AssignManager(Guid? managerEmployeeId)
    {
        ManagerEmployeeId = managerEmployeeId;
        MarkUpdated();
    }
}

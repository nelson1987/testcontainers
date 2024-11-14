namespace Presentation.Commons;

public record CreatedUserEvent(int Id, string Name, int Age, bool IsActive);
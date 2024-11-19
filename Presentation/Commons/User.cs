namespace Presentation.Commons;

public class User
{
    protected User()
    {

    }
    public User(int Id, string Name, int Age, bool IsActive)
    {
        this.Id = Id;
        this.Name = Name;
        this.Age = Age;
        this.IsActive = IsActive;
    }
    public int Id { get; private set; }
    public string Name { get; private set; }
    public int Age { get; private set; }
    public bool IsActive { get; private set; }
}
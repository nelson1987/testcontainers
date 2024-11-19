using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Presentation.Commons;

namespace Presentation.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class ContratosController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<ContratosController> _logger;
    private readonly IMessageProducer<CreatedUserEvent> _createUserProducer;
    private readonly IUserDomainService _userRepository;

    public ContratosController(ILogger<ContratosController> logger,
        IMessageProducer<CreatedUserEvent> createUserProducer,
        IUserDomainService userRepository)
    {
        _logger = logger;
        _createUserProducer = createUserProducer;
        _userRepository = userRepository;
    }

    [HttpGet]
    public async Task<IEnumerable<WeatherForecast>> Get()
    {
        await _createUserProducer.SendMessage(new CreatedUserEvent(5, "John Doe", 18, true));
        await _userRepository.CreateUserAsync(new User(0, "John Doe", 18, true));
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
        
    }
}

public interface IBaseRepository<TEntity> : IDisposable
    where TEntity : class
{
    Task InsertAsync(TEntity t);
    Task UpdateAsync(TEntity t);
    Task DeleteAsync(TEntity t);
    Task<List<TEntity>> FindAllAsync(Func<TEntity, bool> expression);
    Task<TEntity?> FindAsync(Func<TEntity, bool> expression);
    Task<TEntity?> FindByIdAsync(int id);
}

public abstract class BaseRepository<TEntity> : IBaseRepository<TEntity>
    where TEntity : class
{
    private readonly MyContext _myContext;

    public BaseRepository(MyContext myContext)
    {
        _myContext = myContext;
    }

    public void Dispose()
    {
        _myContext.Dispose();
    }

    public async Task InsertAsync(TEntity t)
    {
        _myContext.Entry(t).State = EntityState.Added;
        await _myContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(TEntity t)
    {
        _myContext.Entry(t).State = EntityState.Modified;
        await _myContext.SaveChangesAsync();
    }

    public Task DeleteAsync(TEntity t)
    {
        throw new NotImplementedException();
    }

    public async Task<List<TEntity>> FindAllAsync(Func<TEntity, bool> expression)
    {
        return _myContext.Set<TEntity>().Where(expression).ToList();
    }

    public async Task<TEntity?> FindAsync(Func<TEntity, bool> expression)
    {
        return _myContext.Set<TEntity>().Where(expression).FirstOrDefault();
    }

    public async Task<TEntity?> FindByIdAsync(int id)
    {
        var findEntity = await _myContext.Set<TEntity>().FindAsync(id);
        return findEntity;
    }
}

public interface IUserRepository
    : IBaseRepository<User>
{
}

public class UserRepository
    : BaseRepository<User>, IUserRepository
{
    //atributo para armazenar o contexto..
    private readonly MyContext contexto;

    //construtor para injeção de dependencia..
    public UserRepository(MyContext contexto)
        : base(contexto)
    {
        this.contexto = contexto;
    }
}

public interface IUnitOfWork
{
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();

    #region Repositorios

    IUserRepository Users { get; }

    #endregion
}

public class UnitOfWork : IUnitOfWork
{
    private readonly MyContext _myContext;
    private IDbContextTransaction _dbContextTransaction;

    public UnitOfWork(MyContext contexto)
    {
        _myContext = contexto;
    }

    public async Task BeginTransactionAsync()
    {
        _dbContextTransaction = await _myContext.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        await _dbContextTransaction.CommitAsync();
    }

    public async Task RollbackAsync()
    {
        await _dbContextTransaction.RollbackAsync();
    }

    public IUserRepository Users
        => new UserRepository(_myContext);
}

public interface IUserDomainService
{
    Task CreateUserAsync(User user);
}

public class UserDomainService : IUserDomainService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserDomainService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task CreateUserAsync(User user)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();
            await _unitOfWork.Users.InsertAsync(user);
            await _unitOfWork.CommitAsync();
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackAsync();
        }
    }
}
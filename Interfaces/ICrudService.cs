namespace simulationTest.Interfaces;

public interface ICrudService<T> where T : class
{
    T Create(T entity);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> GetByIdAsync(int id);
    Task<T> UpdateAsync(T entity);
    Task<T> DeleteAsync(int id);
}
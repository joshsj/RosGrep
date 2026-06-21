namespace Lib;

public class Repository<T> : IRepository<T> where T : class
{
	public ISet<T> GetAll() => new HashSet<T>();

	public T? GetById(int id) => default;

	public void Add(T _) { }

	public int Save() => 0;
}


namespace Lib;

public interface IRepository<T> where T : class
{
	bool IsConnected { get; }

	ISet<T> GetAll();

	T? GetById(int id);

	void Add(T entity);

	int Save();
}


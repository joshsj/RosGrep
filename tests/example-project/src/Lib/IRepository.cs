namespace Lib;

public interface IRepository<T> where T : class {
	ISet<T> GetAll();

	T? GetById(int id);

	void Add(T entity);

	int Save();
}


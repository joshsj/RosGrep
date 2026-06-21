using Lib;

namespace App;

public class Service1(IRepository<Foo> repo) {
	public void Method() {
		repo.Add(new Foo());
		repo.Save();
	}
}

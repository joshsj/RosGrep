using Lib;

namespace App;

public class Service2(Service1 service1, IRepository<Bar> repo) {
	public void Method() {
		service1.Method();

		repo.Add(new Bar());
		// forget to save
	}
}

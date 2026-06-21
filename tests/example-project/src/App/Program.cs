using Lib;

namespace App;

public class Program {
	public static void Main() {
		var fooRepo = new Repository<Foo>();
		var barRepo = new Repository<Bar>();

		var service1 = new Service1(fooRepo);
		var service2 = new Service2(service1, barRepo);

		service1.Method();
		service2.Method();

		// catch the missing save in service 2
		barRepo.Save();
	}
}


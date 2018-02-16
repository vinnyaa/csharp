using System;

namespace CSSerialLibrary {
	public class SerialWork {

		public static void Main(String[] args) {
			dothings();
			dothings2();
		}

		public static void dothings() {
			Console.WriteLine("This is the textchanged branch");
			Console.WriteLine("this branch should have text changed and new method");
		}

		public static void dothings2() {
			Console.WriteLine("Dothings2");
		}
	}
}
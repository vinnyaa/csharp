using System;
using System.IO.Ports;

namespace CSSerialLibrary {
	public class Teensy {

		public static void Main(String[] args) {
			new SerialWork("ttyS2");
		}

		public SerialWork(string port, int baudrate) {
			SerialPort teensy = new SerialPort(port, baudrate);
			teensy.Open();
		}
	}
}
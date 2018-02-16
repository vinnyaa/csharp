using System;
using System.IO.Ports;

namespace CSSerialLibrary {
	public class Teensy {
		private static SerialPort port;

		public static void Main(String[] args) {
			Teensy t = new Teensy("ttyS2", 9600, DataReceivedHandler);
			byte[] newdata = new byte[64] 
				{
					0xAA, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
					0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
					0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
					0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
					0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
					0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
					0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
					0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0xEE,
				};
			while (Console.ReadLine() != "send");
			t.Write(newdata);
			while (Console.ReadLine() != "off");
			t.off();
		}

		private static void DataReceivedHandler( object sender, SerialDataReceivedEventArgs e) {
	        port = (SerialPort) sender;
	        // string indata = sp.ReadExisting();
	        byte[] data = new byte[64];
			port.Read(data, 0, 64);
	        Console.WriteLine("Data Received from {0}", e.PortName);
	        // Console.Write(indata);
	        for (int i = 0; i < data.Length; i++) {
	        	Console.WriteLine("Data[{0}] = " + data[i].ToString("X2"), i);
	        }
	    }

	    public void off() {
			teensy.Close();
		}

		public Teensy(string port, int baudrate, Action<object, SerialDataReceivedEventArgs> method) {
			port = new SerialPort(port, baudrate);
			port.Open();
			port.DiscardInBuffer();
			port.DiscardOutBuffer();
			port.DataReceived += new SerialDataReceivedEventHandler(method);
		}

		public void Write(byte[] data) {
			this.port.Write(data, 0, data.Length);
		}
	}
}

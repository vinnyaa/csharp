using System;
using System.IO.Ports;

namespace CSSerialLibrary {
	public class Teensy {

		public static void Main(String[] args) {
			new Teensy("ttyS2", 9600);
		}

		public Teensy(string port, int baudrate) {
			SerialPort teensy = new SerialPort(port, baudrate);
			teensy.Open();
			teensy.DiscardInBuffer();
			teensy.DiscardOutBuffer();
			teensy.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
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
			teensy.Write(newdata, 0, 64);
			teensy.Close();
		}

		private static void DataReceivedHandler( object sender, SerialDataReceivedEventArgs e) {
	        SerialPort sp = (SerialPort)sender;
	        // string indata = sp.ReadExisting();
	        byte[] data = new byte[64];
			sp.Read(data, 0, 64);
	        Console.WriteLine("Data Received:");
	        // Console.Write(indata);
	        for (int i = 0; i < data.Length; i++) {
	        	Console.WriteLine("Data[{0}] = " + data[i].ToString("X2"), i);
	        }
	    }
	}
}

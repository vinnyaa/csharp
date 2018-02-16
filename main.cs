using System;
using ECEServer;
using System.IO;
using System.Threading;

public class ChatMain {

	public static void Main(String[] args) {
		string command;
		CommServer server = new CommServer("192.168.7.1", 9009, 9008, 9007);
		server.OnBytesReceived += RespondToIncoming;

		// TEST FOR SENGIND A FILE TO BBB
		// the next three lines require a temp.txt file to be
		// present in the local directory
		// string path = Directory.GetCurrentDirectory();
		// path = path + "/temp.txt";
		// server.sendFile(path);

		// 1 <= deviceID <= 4
		int deviceID = 1;
		// server.turnPower(true, 1);

		// TEST FOR RECIEVE BYTES
		// byte[] things = server.receiveBytes(deviceID);
		// for(int i = 0; i < things.Length; i++) {
			// Console.WriteLine(things[i].ToString("X"));
		// }

		// TEST FOR SEND BYTES
		// Note that the actual function you would use would
		// be server.sendBytes(int id, byte[] data) 
		// where id is the emm'd device id and data 
		// is the data to be sent to the emm'd device
		// Console.WriteLine()
		// Console.WriteLine("TESTING SEND BYTES...");
		// server.testSendBytes(deviceID);
		// Console.WriteLine("SEND BYTES TESTING COMPLETE");

		// TEST GETVID()
		// Console.WriteLine("Getting VID...");
		// Console.WriteLine(server.getVID(deviceID));

		// TEST GETPID()
		// Console.WriteLine("Getting PID");
		// Console.WriteLine(server.getPID(deviceID));
		// System.Threading.Thread.Sleep(2000);
		// server.turnPower(false, 1);
		
		bool run = true; 
		while (run) {
			command = Console.ReadLine();
			if (command == "off") {
				run = !run;
			} else if (command == "send") {
				command = Console.ReadLine();
				server.sendData(command);
			} else if (command == "test") {
				server.testSendBytes(deviceID);
			} else if (command == "chat") {
				server.sendData("RELAY:");
			} else if (command == "POWER"){
				server.turnPower(true,0);
			}
		}

		server.disconnect();
	}

	public static void RespondToIncoming(object sender, ECEServer.ByteEventArgs args) {
		Console.WriteLine("!Receiving bytes events triggered!");
		Console.WriteLine("Data came from device: " + args.device);
		byte[] bytes = args.data;
		foreach (byte each in bytes) {
            Console.WriteLine(each.ToString("X"));
            Console.Out.Flush();
        }
	}
}

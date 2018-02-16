using System ; 
using System.Net;
using System.Net.Sockets ; 
using System.IO ; 
using System.Diagnostics;
using System.Collections;
using System.Security.Permissions;
using System.Globalization;
using System.Threading;
using CSSerialLibrary;
public class ClientSocket {
    
    protected NetworkStream networkStream; 
    protected NetworkStream sendStream;
    protected NetworkStream getStream;

    protected StreamReader streamreader; 
    protected StreamWriter streamwriter;
    protected StreamWriter sendwriter;
    protected StreamReader getreader;

    protected ArrayList serialDevices;

    protected FileSystemWatcher watcher;

    protected string servermessage = "";
    protected byte[] bytes =  new byte[32] {0x20, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50};
    
    public static void Main(string[] args) {
        ClientSocket cs = new ClientSocket();
    }

    private void configureTeensys() {
        string[] lines = FILE.ReadAllLines(Directory.GetCurrentDirectory() + @"/configSerial.txt");
        serialDevices = new ArrayList();
        //TODO create onTeensySent
        foreach (string name in lines) {
            serialDevices.Add(new Teensy(name, 9600, onTeensySent));
        }
        Console.WriteLine("Configured {0} Teensy devices as usb client", serialDevices)
    }
    
    [PermissionSet(SecurityAction.Demand, Name="FullTrust")]
    public ClientSocket() { 
        TcpClient server;
        TcpClient sendServer;
        TcpClient getServer;
        bool status = true ; 
        try { 
        // the 192 address is for ethernet over usb
        server = new TcpClient("192.168.7.1", 9009);
        sendServer = new TcpClient("192.168.7.1", 9008);
        getServer = new TcpClient("192.168.7.1", 9007);
        // the 10 address is for the ethernet only connection
        // server = new TcpClient("10.42.0.1", 9009);
        Console.WriteLine("Connected to Server");
        } catch { 
            Console.WriteLine("Failed to Connect to server"); 
            return ; 
        } 
        networkStream = server.GetStream(); 
        sendStream = sendServer.GetStream();
        getStream = getServer.GetStream();

        streamreader = new StreamReader(networkStream) ; 
        streamwriter = new StreamWriter(networkStream) ;
        sendwriter = new StreamWriter(sendStream);
        getreader = new StreamReader(getStream);
        Console.WriteLine("Streams Created");
        try 
        { 
            // this while loop recieves commands for the duration
            // that the beaglebone is running
            while(status) 
            { 
                servermessage = getData(); 
                
                switch (servermessage) {
                    case "VID:":
                        getVID();
                        break;
                    case "PID:":
                        getPID();
                        break;
                    case "DEVICES:":
                        getDevices();
                        break;
                    case "TransmitReady:":
                        saveFile();
                        break;
                    case "GOTBYTE:":
                        receiveBytes();
                        break;
                    case "POWER:":
                        turnPower();
                        break;
                    case "medic:":
                        getMedData();
                        break;                        
                    case "bye":
                        status = false;
                        break;
                    case "red":
                        runScript("usr0.sh");
                        break;
                    case "blue":
                        runScript("usr1.sh");
                        break;
                    case "green":
                        runScript("usr2.sh");
                        break;
                    case "watchdogs":
                        Thread th = new Thread(watchdog);
                        th.Start();
                        break;
                        
                    default:
                        Console.WriteLine("Host: " + servermessage);
                        break;
                }
            } 
        } 
        catch
        { 
            Console.WriteLine("Exception reading from the server") ; 
        } 
        streamreader.Close(); 
        getreader.Close();
        streamwriter.Close();
        // sendwriter.Close(); 
        networkStream.Close();
        // sendStream.Close();
        getStream.Close(); 
    }

    protected string getData() {
        return streamreader.ReadLine();
    }

    protected void getMedData() {
        runScript("medicalReader.py");
        string path = Directory.GetCurrentDirectory() + "/usbOut.txt";
        sendFile(path);
    }
    
    protected void getDevices() {
        ArrayList devices = new ArrayList();
        devices.Add(1);
        // need a real way to get devices here
        foreach(var item in devices) {
            sendData("" + item);
        }
        sendData("TransmitOver:");
    }

    protected void getPID() {
        int device = int.Parse(getData());
        string[] lines = File.ReadAllLines(Directory.GetCurrentDirectory() + @"/vid.txt");
        // need to get real pid here
        sendData(lines[1]);
    }
    
    protected void getVID() {
        int device = int.Parse(getData());
        string[] lines = System.IO.File.ReadAllLines(Directory.GetCurrentDirectory() + @"/vid.txt");
        // need to get real vid here
        sendData(lines[0]);
    }
    
    protected void receiveBytes() {
        string device = getData();
        int size = int.Parse(getData());
        byte[] result = new byte[size];
        getStream.Read(result, 0, size);
        // need to do something with received bytes here
        Console.WriteLine("Received Bytes to send to: " + device);
        for(int i = 0; i < size; i++) {
            Console.WriteLine(result[i].ToString("X"));
        }
        sendPyBytes(result);
    }

    protected void runScript(string shell) {
        Process p = new Process();
        p.StartInfo.FileName = shell;
        p.Start();
        p.WaitForExit();
    }
    
    protected void saveFile() {
        servermessage = getData();
        ArrayList lines = new ArrayList();
        while(servermessage != "TransmitOver:"){
            lines.Add(servermessage);
            servermessage = getData();
        }
        string[] myArr = (string[]) lines.ToArray(typeof(string));
        System.IO.File.WriteAllLines(Directory.GetCurrentDirectory() + @"/temp.txt", myArr);
    }

    protected void sendBytes( byte[] byteArray) {
        // need to actually get real bytes here
        sendwriter.WriteLine(byteArray.Length);
        sendStream.Write(byteArray, 0, byteArray.Length);
    }
    
    protected bool sendData(string data) {
        streamwriter.WriteLine(data) ;
        streamwriter.Flush() ; 
        return true;
    }

    protected bool sendFile(string fileName) {
        Console.WriteLine(fileName);
        if(!File.Exists(fileName)) {
            Console.Write("!FILE NOT FOUND!");
            return false;
        }
        char[] splitOn = {'/'};
        string[] splitName = fileName.Split(splitOn);
        // seperated the path from the aaa.txt we want here
        sendData(splitName[splitName.Length - 1]);
        string[] lines = System.IO.File.ReadAllLines(fileName);
        foreach (string line in lines) {
            sendData(line);
        }
        sendData("TransmitOver:");
        return true;
    }

    protected static void sendTeensy(int device, byte[] data) {

    }

    protected void turnPower() {
        string onOrOff = getData();
        int id = int.Parse(getData());
        // need to actually turn the devices on or off here
        Console.WriteLine("Device " + id + ": " + onOrOff);
        if ((id < 1) || (id > 4)) {
            Console.WriteLine("Invalid device Specified");
        }
        //temporary here
        if(id != 1) {
            Console.WriteLine("Usually this would work, but only the shell script for usr0.sh is present");
            return;
        }
        string script = "usr" + (id-1) + ".sh";
        runScript(script);
        if(onOrOff == "on:") {
            // turn thing with id 'device' on
        } else if(onOrOff == "off:") {
            // turn thing with id 'device' off
        }
    }
    
    protected void watchdog() {
        runScript("demo.py");
    }
} 

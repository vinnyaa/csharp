using System ;
using System.Net;
using System.Net.Sockets ;
using System.Collections;
using System.IO ; 
using System.Threading;

namespace ECEServer {
    public class ByteEventArgs : EventArgs {
        public byte[] data { get; set; }
        public int device { get; set; }
        public ByteEventArgs(int inDevice, byte[] inData) {
            data = inData;
            device = inDevice;
        }
    }

    public class CommServer
    {

        protected IPAddress ipa;
        protected int port;
        protected int receivingPort;
        protected int sendingPort;

        protected TcpListener tcpListener;
        protected TcpListener getTcpListener;
        protected TcpListener giveTcpListener;

        protected TcpClient client;
        protected TcpClient getClient;
        protected TcpClient giveClient;

        protected NetworkStream networkStream;
        protected NetworkStream getStream;
        protected NetworkStream giveStream;

        protected StreamWriter streamwriter;
        protected StreamReader streamreader;
        protected Thread incomingThread;
        public event EventHandler<ByteEventArgs> OnBytesReceived;

        
        // protected BinaryWriter binarywriter;
        // protected BinaryReader binaryreader;
        /*  
            DESCRIPTION:
            Constructor for the class. Requres an ipaddress
            and a portnumber to be given. Should match the
            ipaddress of the computer's LAN ethernet address.

            PARAMETERS:
            ipa: ethernet outgoing IPAdress
                ie: "10.42.0.1" (actual ethernet port)
                ie: "192.168.7.1" (ethernet over usb)
            portnum: port number to start server on*
                ie: 9009
            secondPort: port number for the bone to send byte[] to
            thirdPort: port number used for this computer to send byte[] to the beaglebone
            *The port number has been set on the beaglebone
            and would have to be changed there if using a port
            number other that 9009.
        */
        public CommServer(string ipas, int portnum, int secondPort, int thirdPort)
        {
            ipa = IPAddress.Parse(ipas);
            port = portnum;
            receivingPort = secondPort;
            sendingPort = thirdPort;
            connect();
        }

        /*
            DESCRIPTION:
            Tries to connect using the given ipaddress and
            port number. Throws an exception if either are
            invalid. 
            
            PARAMETERS:

            NOTE: Can be used after .disconnect() to restore
            a connection on the stored ipaddress & port number
        */
        public bool connect() {
            try
            {
                // for sending strings and commands
                tcpListener = new TcpListener(ipa, port);
                tcpListener.Start();

                // for receiving byte[] data from bone (connection controlled by bone)
                getTcpListener = new TcpListener(ipa, receivingPort);
                getTcpListener.Start();
                
                // for sending byte[] data to bone (paired with command)
                giveTcpListener = new TcpListener(ipa, sendingPort);
                giveTcpListener.Start();

                Console.WriteLine("Server Started") ;

                client = tcpListener.AcceptTcpClient() ;
                getClient = getTcpListener.AcceptTcpClient();
                giveClient = giveTcpListener.AcceptTcpClient();

                Console.WriteLine("Bone Connected") ;
                networkStream = client.GetStream();
                getStream = getClient.GetStream();
                giveStream = giveClient.GetStream();

                streamwriter = new StreamWriter(networkStream);
                streamreader = new StreamReader(networkStream);
                incomingThread = new Thread(receiveBytes);
                incomingThread.Start();
                
                watchdog();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString()) ;
                return false;
            }
            return true;
        }


        /*
            DESCRIPTION:
            Closes the connection with the client and with the
            associated stream objects.

            PARAMETERS:

            NOTE: Does not delete ipaddress or the port number,
            so the connection can be reinstated.
        */
        public bool disconnect() {
            Console.WriteLine("Bone Disconnecting...");
            sendData("bye");
            incomingThread.Abort();
            client.Close();
            getClient.Close();
            giveClient.Close();
            streamwriter.Close();
            streamreader.Close();
            networkStream.Close();
            getStream.Close();
            giveStream.Close();
            return true;
        }


        /*
            DESCRIPTION:
            This is the way to access the numerical (int) ids 
            for the possible medical devices. This will return an
            arraylist of all medical devices WHETHER THEY ARE 
            TURNED ON OR OFF
        */
        public ArrayList findAvailableIDs() {
            ArrayList devices = new ArrayList();
            if (isConnected()) {
                sendData("DEVICES:");
                string message = streamreader.ReadLine();
                while(message != "TransmitOver:"){
                    devices.Add(int.Parse(message));
                    message = getData();
                }
                return devices;
            }
            return null;
        }

        public void usbTest() {
            sendData("medic:");
            Console.WriteLine("Waiting for data to be generated...");
            string msg = getData();
            Console.WriteLine("Saving data to file: " + msg);
            saveFile(msg);
        }

        protected string getData() {
            return streamreader.ReadLine();
        }

        public string getPID(int device) {
            sendData("PID:");
            sendData("" + device);
            Console.WriteLine("Waiting on PID...");
            string pid = getData();
            return pid;
        }

        public string getVID(int device) {
            sendData("VID:");
            sendData("" + device);
            Console.WriteLine("Waiting on VID...");
            string vid = getData();
            return vid;
        }

        public bool isConnected() {
            return client.Connected;
        }

        /*
            DESCRIPTION:
            This is meant to be used when you would expect a device to
            have already received data. This will fetch the waiting 
            data from the bone and return it in a byte[]
        */
        protected void receiveBytes() {
            // StreamReader reader = new StreamReader(getStream);
            int device;
            int size;
            Console.WriteLine("Thread has started");
            Console.WriteLine("Connected?: " + getStream.CanRead);
            try {
                while (true) {
                    device = getStream.ReadByte();
                    Console.WriteLine("Data from device: " + device);
                    size = getStream.ReadByte();
                    Console.WriteLine("Bytes: " + size);
                    byte[] result = new byte[size];
                    getStream.Read(result, 0, size);
                    OnBytesReceived(this, new ByteEventArgs(device, result));
                    Console.WriteLine("OnBytesReceived event should be thrown by now");
                }
            } catch (Exception e) {
                Console.WriteLine("I caught an error:" + e.GetType().FullName);
            }
        }


        protected void saveFile(string fileName) {
            string servermessage = getData();
            ArrayList lines = new ArrayList();
            while(servermessage != "TransmitOver:"){
                lines.Add(servermessage);
                servermessage = getData();
            }
            string[] myArr = (string[]) lines.ToArray(typeof(string));
            System.IO.File.WriteAllLines(Directory.GetCurrentDirectory() + @"/" + fileName, myArr);
        }


        /*
            DESCRIPTION:
            This function is the one that will send bytes to the bone
            and later to the medical device with the numerical id 
            provided as an int argument. This is the only way to 
            transfer bytes to the bone.

            PARAMS:
            id: integer identifying the specific medical device to 
            send the byte[] to. 
        */
        public void sendBytes(int id, byte[] data) {
            sendData("GOTBYTE:");
            sendData(id + "");
            sendData("" + data.Length);
            // need to actually get real bytes here
            giveStream.Write(data, 0, 32);
        }

        /*
            DESCRIPTION:
            This is a function that should only be used by other
            functions within this file in order to carry out other
            comands.

            PARAMS:
            data: the string that will be sent to the bone exactly the 
            way it is entered here. 
        */
        public bool sendData(string data) {
            if(isConnected())
            {
                streamwriter.WriteLine(data) ;
                streamwriter.Flush() ; 
                return true;
            } 
            return false;
        }

        /*
            DESCRIPTION:
            This function transfers a file from the computer this
            is running on to the beaglebone device that is connected

            PARAMS:
            fileName: Name of the path&file to transfer, must be in the
            specified directory. ie: "c:/docs/temp.txt"
        */
        public bool sendFile(string fileName) {
            if(!File.Exists(fileName)) {
                Console.WriteLine("!FILE NOT FOUND!");
                return false;
            }
            string[] lines = System.IO.File.ReadAllLines(fileName);
            sendData("TransmitReady:");
            foreach (string each in lines) {
                sendData(each);
            }
            sendData("TransmitOver:");
            return true;
        }

        public void testSendBytes(int id) {
            byte[] bytes =  new byte[32] {0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x40, 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47, 0x48, 0x49, 0x50, 0x20, 0x20, 0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29, 0x30, 0x31, 0x32};
            
            sendBytes(id, bytes);
        }

        /*
            DESCRIPTION:
            This function will individually turn the power of a medical
            device attached to the bone either on or off, depending on 
            the value of bool on. If on = true, the device will turn 
            on. If on = false, the device will turn off.

            PARAMS: 
            bool on: boolean that determines if you are turning the 
            device on or off
            int device: This number identifies the number associated 
            with the device to turn on. Numbers available will be 
            provided by findAvailableDevices()
        */
        public bool turnPower(bool on, int device) {
            if(isConnected()) {
                sendData("POWER:");
                string message = (on) ? "on:" : "off:";
                sendData(message);
                sendData("" + device);
                return true;
            }
            return false;
        }
        
        public void watchdog() {
            sendData("watchdogs");
        }
    } 
}

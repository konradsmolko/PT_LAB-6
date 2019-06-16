using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;

namespace lab6
{
    /// <summary>
    /// Logika interakcji dla klasy App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Window thisWindow;

        public static void InitializeApp(Window win) => thisWindow = win; // po co?
    }

    public class Server
    {
        private readonly List<int> clientIDs = new List<int>();
        private readonly ConcurrentDictionary<int, IPEndPoint> clientListeners = new ConcurrentDictionary<int, IPEndPoint>();
        private readonly ConcurrentQueue<Point> drawingQueue = new ConcurrentQueue<Point>();
        private Canvas canvas;
        private bool isRunning = false;
        UdpClient globalUDPListener;
        IPEndPoint groupEP;
        Task handler, drawer;

        public bool IsRunning { get => isRunning; }

        public Server()
        {
            
        }

        public int Start(Canvas c, string hostname, int port)
        {
            try
            {
                if (hostname == "localhost")
                    //groupEP = new IPEndPoint(IPAddress.Any, port);
                    groupEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
                else
                    groupEP = new IPEndPoint(IPAddress.Parse(hostname), port);
                
                globalUDPListener = new UdpClient(port);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Server failed to start!\nHostname: {hostname}\nPort: {port}");
                Console.WriteLine(e.ToString());
                return 1;
            }

            isRunning = true;
            canvas = c;

            handler = Task.Factory.StartNew(() => HandleClients());
            drawer = Task.Factory.StartNew(() => DrawReceivedPoints());
            Console.WriteLine($"Server started.\nAddress: {groupEP.ToString()}");
            return 0;
        }

        private void HandleClients()
        {
            while (isRunning || globalUDPListener.Available != 0) // handle clients until the server is stopped, and prevent any client from leaving their crap in the buffer
            {
                if (globalUDPListener.Available == 0) // this is here to prevent blocking (until a client gets processed) when we want to stop the server
                {
                    Thread.Sleep(10);
                    continue;
                }
                Console.WriteLine("Handling client...");

                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, groupEP.Port);
                byte[] data = globalUDPListener.Receive(ref remoteEP);
                Console.WriteLine($"Server got a connection!\n\tClient: {remoteEP.ToString()}");
                switch (Encoding.ASCII.GetString(data))
                {
                    case "connect": // respond with personalized listening port #
                        clientListeners.TryAdd(remoteEP.Port, remoteEP);
                        byte[] buf = BitConverter.GetBytes(remoteEP.Port);
                        globalUDPListener.Send(buf, buf.Length, remoteEP);
                        break;

                    case "disconnect": // remove client from list
                        // todo
                        break;

                    default:
                        if (clientListeners.ContainsKey(remoteEP.Port))
                        {
                            // an already connected client sent us some data...

                        }
                        // if that client isn't in our dictionary, we ignore it
                        break;
                }
                //Console.WriteLine(Encoding.ASCII.GetString(data));
                // todo
            }
        }

        private void DrawReceivedPoints()
        {
            while (isRunning || !drawingQueue.IsEmpty)
            {
                
                //canvas.Children.Add();
            }
        }

        public int Stop()
        {
            isRunning = false;
            Console.WriteLine("Server awaiting clients...");
            handler.Wait(); // awaiting any clients being handled right now
            drawer.Wait();
            handler.Dispose();
            drawer.Dispose();

            // todo: send info to all connected clients that the bar is now closed and they need to find an another place to drink

            globalUDPListener.Close();

            Console.WriteLine("Server stopped.");
            return 0;
        }

        private int FindFreeUDPPort()
        {
            var startingAtPort = 5000;
            var maxNumberOfPortsToCheck = 500;
            var range = Enumerable.Range(startingAtPort, maxNumberOfPortsToCheck);
            var portsInUse =
                from p in range
                join used in System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners()
            on p equals used.Port
                select p;

            var FirstFreeUDPPortInRange = range.Except(portsInUse).FirstOrDefault();

            if (FirstFreeUDPPortInRange > 0)
            {
                return FirstFreeUDPPortInRange;
            }
            else
            {
                return -1;
            }
        }
    }

    public class Client
    {
        private UdpClient client = new UdpClient();
        private IPEndPoint outgoingEndpoint;

        public Client()
        {

        }

        public int Connect(string hostname, int port)
        {
            if (hostname == "localhost") hostname = "127.0.0.1";

            try
            {
                outgoingEndpoint = new IPEndPoint(IPAddress.Parse(hostname), port);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Client failed to connect! Hostname: {hostname}");
                Console.WriteLine(e.ToString());
                return 1;
            }
            
            
            byte[] buf = Encoding.ASCII.GetBytes("connect");
            client.Send(buf, buf.Length, outgoingEndpoint);

            Console.WriteLine($"Client connected. Endpoint: {outgoingEndpoint.ToString()}");
            return 0;
        }

        public void Disconnect()
        {
            byte[] buf = Encoding.ASCII.GetBytes("disconnect");
            client.Send(buf, buf.Length, outgoingEndpoint);
            outgoingEndpoint = null;

            Console.WriteLine($"Client disconnecting. Endpoint: {outgoingEndpoint.ToString()}");
        }

        public void StartSending(Point start)
        {

        }

        public void ContinueSending(Point point)
        {

        }

        public void StopSending()
        {

        }
    }
}

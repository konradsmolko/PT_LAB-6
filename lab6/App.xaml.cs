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
        private readonly ConcurrentQueue<Point> drawingQueue = new ConcurrentQueue<Point>();
        private Canvas canvas;
        private bool isRunning = false;
        UdpClient listener;
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
                
                listener = new UdpClient(port);
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
            while (isRunning || listener.Available != 0) // handle clients until the server is stopped, and prevent any client from leaving their crap in the buffer
            {
                if (listener.Available == 0) continue; // this is here to prevent blocking (until a client gets processed) when we want to stop the server
                Console.WriteLine("Handling client...");
                byte[] data = listener.Receive(ref groupEP);
                switch (Encoding.ASCII.GetString(data))
                {
                    case "connect": // respond with personalized listening port #
                        break;
                    case "disconnect": // remove client from list
                        break;
                    default: // handle point collection
                        break;
                }
                // Console.WriteLine(Encoding.ASCII.GetString(data));
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
            listener.Close();

            Console.WriteLine("Server stopped.");
            return 0;
        }
    }

    public class Client
    {
        private Guid guid = Guid.NewGuid(); // used for absolute unique client identification
        private UdpClient client = new UdpClient();
        IPEndPoint endpoint;

        public Client()
        {

        }

        public int Connect(string hostname, int port)
        {
            if (hostname == "localhost") hostname = "127.0.0.1";

            try
            {
                endpoint = new IPEndPoint(IPAddress.Parse(hostname), port);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Client failed to connect!\nGUID: {guid}\n{hostname}");
                Console.WriteLine(e.ToString());
                return 1;
            }
            
            
            byte[] buf = Encoding.ASCII.GetBytes("connect");
            client.Send(buf, buf.Length, endpoint);

            Console.WriteLine($"Client connected.\nGUID: {guid}\nEndpoint: {endpoint.ToString()}");
            return 0;
        }

        public void Disconnect()
        {
            byte[] buf = Encoding.ASCII.GetBytes("disconnect");
            client.Send(buf, buf.Length, endpoint);
            endpoint = null;

            Console.WriteLine($"Client disconnecting.\nGUID: {guid}");
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

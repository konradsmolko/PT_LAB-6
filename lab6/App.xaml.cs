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
using System.IO;
using System.Xml.Serialization;

namespace lab6
{
    public partial class App : Application
    {
        private static Window thisWindow;

        public static void InitializeApp(Window win) => thisWindow = win; // po co?
    }

    // A helper for passing the Point data through UDP. Not mine.
    public static class SerializeHelper
    {
        public static string XmlSerializeToString(this object objectInstance)
        {
            var serializer = new XmlSerializer(objectInstance.GetType());
            var sb = new StringBuilder();

            using (TextWriter writer = new StringWriter(sb))
            {
                serializer.Serialize(writer, objectInstance);
            }

            return sb.ToString();
        }

        public static T XmlDeserializeFromString<T>(this string objectData)
        {
            return (T)XmlDeserializeFromString(objectData, typeof(T));
        }

        public static object XmlDeserializeFromString(this string objectData, Type type)
        {
            var serializer = new XmlSerializer(type);
            object result;

            using (TextReader reader = new StringReader(objectData))
            {
                result = serializer.Deserialize(reader);
            }

            return result;
        }
    }

    public class Server
    {
        // private readonly List<int> clientIDs = new List<int>();
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
                // this is here to prevent blocking (until a client gets processed) when we want to stop the server
                if (globalUDPListener.Available == 0) continue;
                Console.WriteLine("Handling client...");

                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, groupEP.Port);
                byte[] data = globalUDPListener.Receive(ref remoteEP);
                string message = Encoding.ASCII.GetString(data);
                Console.WriteLine($"Server got data from client: {remoteEP.ToString()}");
                switch (message)
                {
                    case "connect": // respond with their listening port #
                        AddClient(remoteEP);
                        byte[] buf = BitConverter.GetBytes(remoteEP.Port);
                        globalUDPListener.Send(buf, buf.Length, remoteEP);
                        break;

                    case "disconnect": // remove client from dict
                        clientListeners.TryRemove(remoteEP.Port, out remoteEP);
                        break;

                    default:
                        if (clientListeners.ContainsKey(remoteEP.Port))
                        {
                            // an already connected client sent us some data...
                            Point point = SerializeHelper.XmlDeserializeFromString<Point>(message);
                            drawingQueue.Enqueue(point);
                        }
                        else // if that client isn't in our dictionary, we ignore it
                            Console.WriteLine($"Server: Something sent me weird data:\n\t{message}");
                        break;
                }
                // todo
            }
        }

        private bool AddClient(IPEndPoint client)
        {
            return clientListeners.TryAdd(client.Port, client);
        }

        private bool RemoveClient(IPEndPoint client)
        {
            return clientListeners.TryRemove(client.Port, out client);
        }

        private void DrawReceivedPoints()
        {
            while (isRunning || !drawingQueue.IsEmpty)
            {
                if (drawingQueue.TryDequeue(out Point point)) BroadcastMessage(point);
            }
        }

        private void BroadcastMessage(Point point)
        {
            string message = point.XmlSerializeToString();
            byte[] buf = Encoding.ASCII.GetBytes(message);

            foreach (IPEndPoint client in clientListeners.Values)
            {
                globalUDPListener.Send(buf, buf.Length, client);
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

            // send info to all connected clients that the bar is now closed and they need to find an another place to drink
            string message = "We're closing, find another place to drink.";
            byte[] buf = Encoding.ASCII.GetBytes(message);
            Console.WriteLine("Server alerting clients...");
            foreach (IPEndPoint client in clientListeners.Values)
            {
                globalUDPListener.Send(buf, buf.Length, client);
                Console.WriteLine($"\tClient {client.ToString()} alerted.");
            }

            globalUDPListener.Close();
            clientListeners.Clear();

            Console.WriteLine("Server stopped.");
            return 0;
        }
    }

    public class Client
    {
        private UdpClient client = new UdpClient();
        private IPEndPoint outgoingEndpoint;
        private bool connected = false;
        Task receiver;
        Canvas canvas;

        public Client()
        {

        }

        public int Connect(string hostname, int port, Canvas c)
        {
            if (hostname == "localhost") hostname = "127.0.0.1";

            try
            {
                outgoingEndpoint = new IPEndPoint(IPAddress.Parse(hostname), port);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Client failed to connect! Hostname: {hostname}");
                Console.WriteLine(e.Message);
                return 1;
            }

            canvas = c;
            //receiver = Task.Factory.StartNew(() => IncomingPointsHandler());

            byte[] buf = Encoding.ASCII.GetBytes("connect");
            client.Send(buf, buf.Length, outgoingEndpoint);

            connected = true;
            Console.WriteLine($"Client connected. UDPC: {client.Client.LocalEndPoint} Endpoint: {outgoingEndpoint.ToString()}");
            return 0;
        }

        public void Disconnect()
        {
            byte[] buf = Encoding.ASCII.GetBytes("disconnect");
            client.Send(buf, buf.Length, outgoingEndpoint);
            connected = false;

            Console.WriteLine($"Client disconnecting. Endpoint: {outgoingEndpoint.ToString()}");
            outgoingEndpoint = null;
        }

        public void StartSending(Point point)
        {
            if (outgoingEndpoint == null) return;
            string message = point.XmlSerializeToString();
            byte[] data = Encoding.ASCII.GetBytes(message);
            client.Send(data, data.Length, outgoingEndpoint);
        }

        public void ContinueSending(Point point)
        {
            if (outgoingEndpoint == null) return;
            string message = point.XmlSerializeToString();
            byte[] data = Encoding.ASCII.GetBytes(message);
            client.Send(data, data.Length, outgoingEndpoint);
        }

        public void StopSending()
        {

        }

        //public List<Point> IncomingPointsHandler()
        //{
        //    if (!connected) return null;
        //    //while (client.Available == 0) { } // waiting...
        //    List<Point> ret = new List<Point>();
        //    while (client.Available != 0)
        //    {
        //        IPEndPoint remoteEP = outgoingEndpoint;
        //        byte[] data = client.Receive(ref remoteEP);
        //        Console.WriteLine($"remote: {remoteEP} out: {outgoingEndpoint}");
        //        string message = Encoding.ASCII.GetString(data);
        //        Point point = SerializeHelper.XmlDeserializeFromString<Point>(message);
        //        ret.Add(point);
        //        //polyline.Dispatcher.Invoke(() => polyline.Points.Add(point));
        //    }
        //    return ret;
        //}
    }
}

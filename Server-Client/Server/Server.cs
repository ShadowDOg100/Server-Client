using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace FirstSocketApp
{
    public class AsyncServer : Messenger
    {
        private ManualResetEvent allDone = new ManualResetEvent(false);
        private ArrayList states;
        private int nextId = 0;
        private const string FILE_PATH = "../../Files/";
        private const int PORT = 11000;

        public AsyncServer()
        {
            StartListening();
        }

        public void StartListening()
        {
            states = new ArrayList();
            messages = new Queue();

            //Console.Write("Port: ");
            //port = Convert.ToInt32(Console.ReadLine());

            IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, PORT);

            var list = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            messageThread = new Thread(HandleMessages);
            messageThread.Start();

            try
            {
                list.Bind(localEndPoint);
                list.Listen(100);

                while (true)
                {
                    allDone.Reset();
                    Console.WriteLine("Waiting for a connection...");
                    list.BeginAccept(AcceptCallback, list);

                    allDone.WaitOne();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            allDone.Set();

            Socket listener = (Socket) ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            StateObject state = new StateObject {workSocket = handler};

            Send(handler,"login|" + nextId + "");
            state.id = nextId;
            nextId++;

            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                                 ReadCallback, state);
            states.Add(state);
        }

        private void Broadcast(string message)
        {
            foreach (object t in states)
            {
                StateObject s = (StateObject) t;
                Send(s.workSocket, message);
            }
        }

        public override void HandleMessages()
        {
            while (true)
            {
                if (messages.Count == 0)
                    continue;
                Message m = (Message)messages.Dequeue();
                string[] tokens = m.message.Split('|');
                string send = "";
                string path = "";
                switch (tokens[0])
                {
                    case "login":
                        send = tokens[1] + " has logged in with id: " + tokens[2];
                        break;
                    case "chat":
                        send = tokens[1] + ": " + tokens[3];
                        break;
                    case "logout":
                        for (int i = 0; i < states.Count; i++)
                        {
                            StateObject s = (StateObject)states[i];
                            if (s.id == int.Parse(tokens[2]))
                            {
                                s.loggedIn = false;
                                states.RemoveAt(i);
                                break;
                            }
                        }
                        send = tokens[1] + " has logged out.";
                        break;
                    case "send":
                        send = tokens[1] + " sent a file.";

                        path = FILE_PATH + tokens[3];

                        if (!System.IO.File.Exists(path))
                        {
                            FileStream fs = System.IO.File.Create(path);
                            fs.Close();
                        }

                        File.WriteAllBytes(path, m.file);
                        break;
                    case "update":
                        send = "Updating the server.";
                        string[] toks = tokens[3].Split('.');
                        path = toks[0] + "_temp." + toks[1];

                        if (!System.IO.File.Exists(path))
                        {
                            FileStream fs = System.IO.File.Create(path);
                            fs.Close();
                        }

                        File.WriteAllBytes(path, m.file);

                        System.Diagnostics.Process proc = new System.Diagnostics.Process
                            {
                                EnableRaisingEvents = false,
                                StartInfo = {FileName = "bash.sh"}
                            };
                        proc.Start();
                        break;
                    case "recieve":
                        send = "Sending file to " + tokens[1];

                        path = FILE_PATH + tokens[3];
                        if (!System.IO.File.Exists(path))
                        {
                            Console.WriteLine("No such file.");
                            send += " failed";
                            continue;
                        }
                        byte[] bytes = File.ReadAllBytes(path);

                        Socket client = null;
                        foreach (object t in states)
                        {
                            StateObject s = (StateObject)t;
                            if (s.id == int.Parse(tokens[2]))
                            {
                                client = s.workSocket;
                            }
                        }

                        if (client != null)
                        {
                            Send(client, "send|" + path, bytes);
                            send += " succeeded";
                        }
                        else
                        {
                            send += " failed";
                        }

                        break;
                    case "shutdown":
                        Environment.Exit(0);
                        break;
                    default:
                        break;
                }
                Broadcast("chat|" + send);
                Console.WriteLine(send);
            }
        }

        public static int Main(String[] args)
        {
            AsyncServer main = new AsyncServer();
            return 0;
        }
    }
}
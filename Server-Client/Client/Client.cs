using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Client
{
    public class AsyncClient : Messenger
    {
        private int port = 11000;
        private string ip = "127.0.0.1";

        private int id = -1;

        private StateObject state;

        private string name = "";

        public AsyncClient()
        {
            StartClient();
        }

        private void StartClient()
        {
            try
            {
                messages = new Queue();

                Console.Write("IP: ");
                ip = Console.ReadLine();
                Console.Write("Port: ");
                port = Convert.ToInt32(Console.ReadLine());

                IPHostEntry ipHostInfo = Dns.Resolve(ip);
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEp = new IPEndPoint(ipAddress, port);

                Socket client = new Socket(AddressFamily.InterNetwork,
                                           SocketType.Stream, ProtocolType.Tcp);

                messageThread = new Thread(HandleMessages);
                messageThread.Start();

                Console.Write("Name: ");
                name = Console.ReadLine();

                client.BeginConnect(remoteEp, ConnectCallback, client);
                connectDone.WaitOne();

                recieveDone.WaitOne();

                while (id == -1)
                {
                }

                Send(client, "login|" + name + "|" + id + "");
                sendDone.WaitOne();

                while (true)
                {
                    sendDone.Reset();
                    var output = Console.ReadLine();
                    if (output == null)
                        continue;
                    if (output == "logout")
                    {
                        Send(client, "logout|" + name + "|" + id + "");
                        state.loggedIn = false;
                        sendDone.WaitOne();
                        break;
                    } else if (output == "help")
                    {
                        Console.WriteLine("List of Commands:");
                        Console.WriteLine("1. <message> : sends a chat message");
                        Console.WriteLine("2. send <path> : sends a file to the server");
                        Console.WriteLine("3. recieve <path> : downloads a file from the server");
                        Console.WriteLine("4. update : special update command only for update server, used to update main server remotely");
                        Console.WriteLine("5. shutdown: shuts the server down");
                        continue;
                    }
                    string[] toks = output.Split(' ');
                    string path = "";
                    if (toks.Length > 1)
                    {
                        path = toks[1];
                    }
                    if (output == "shutdown")
                    {
                        Send(client, "shutdown");
                    } 
                    else if (output.StartsWith("send"))
                    {
                        if (!System.IO.File.Exists(path))
                        {
                            Console.WriteLine("No such file.");
                            continue;
                        }
                        byte[] bytes = File.ReadAllBytes(path);
                        Send(client, "send|" + name + "|" + id + "|" + path, bytes);
                    } 
                    else if (output.StartsWith("update"))
                    {
                        if (!System.IO.File.Exists(path))
                        {
                            Console.WriteLine("No such file.");
                            continue;
                        }
                        byte[] bytes = File.ReadAllBytes(path);
                        Send(client, "update|" + name + "|" + id + "|" + path, bytes);
                    }
                    else if (output.StartsWith("recieve"))
                    {
                        Send(client, "recieve|" + name + "|" + id + "|" + path);
                    }
                    else
                    {
                        Send(client, "chat|" + name + "|" + id + "|" + output);
                    }
                    sendDone.WaitOne();
                }

                RequestStop();
                client.Shutdown(SocketShutdown.Both);
                client.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                Socket client = (Socket) ar.AsyncState;

                client.EndConnect(ar);

                Console.WriteLine("Socket connected to {0}", client.RemoteEndPoint.ToString());

                state = new StateObject {workSocket = client};

                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                                    ReadCallback, state);

                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public override void HandleMessages()
        {
            while (!_shouldStop)
            {
                if (messages.Count == 0)
                    continue;
                Message m = (Message) messages.Dequeue();
                string[] tokens = m.message.Split('|');
            
                switch (tokens[0])
                {
                    case "login":
                        id = int.Parse(tokens[1]);
                        break;
                    case "chat":
                        if(!tokens[1].StartsWith(name + ":"))
                            Console.WriteLine(tokens[1]);
                        break;
                    case "send":
                        string[] toks = tokens[1].Split('/');
                        string path = toks[toks.Length - 1];
                        if (!System.IO.File.Exists(path))
                        {
                            FileStream fs = System.IO.File.Create(path);
                            fs.Close();
                        }

                        File.WriteAllBytes(path, m.file);
                        Console.WriteLine("Recieved File.");
                        break;
                }
            }
        }

        public static int Main(String[] args)
        {
            AsyncClient main = new AsyncClient();
            return 0;
        }
    }
}
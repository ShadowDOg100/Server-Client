using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class StateObject
{
    public Socket workSocket = null;
    public const int BufferSize = 1024;
    public byte[] buffer = new byte[BufferSize];
    public byte[] bytes;
    public int bytesToRead = -1;
    public int bytesRead = 0;
    public bool loggedIn = true;
    public int id = -1;
    public bool recieving;
}

public class Messenger
{
    public Queue messages;
    public Thread messageThread;

    public Messenger()
    {
        
    }

    protected void ReadCallback(IAsyncResult ar)
    {
        String content = String.Empty;

        StateObject state = (StateObject)ar.AsyncState;
        Socket handler = state.workSocket;

        int bytesRead = handler.EndReceive(ar);

        if (!state.loggedIn)
        {
            handler.Shutdown(SocketShutdown.Both);
            handler.Close();
            return;
        }

        if (bytesRead > 0)
        {
            if (state.bytesToRead == -1)
            {
                byte[] length = new byte[4];
                Buffer.BlockCopy(state.buffer, 0, length, 0, 4);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(length);
                int l = BitConverter.ToInt32(length, 0);
                state.bytesToRead = l;
            }

            if (state.bytes == null)
            {
                state.bytes = new byte[0];
            }

            if (bytesRead + state.bytesRead > state.bytesToRead)
            {
                bytesRead = state.bytesToRead - state.bytesRead;
            }

            byte[] temp = new byte[state.bytes.Length + bytesRead];
            Buffer.BlockCopy(state.bytes, 0, temp, 0, state.bytes.Length);
            Buffer.BlockCopy(state.buffer, 0, temp, state.bytes.Length, bytesRead);
            state.bytes = temp;

            state.bytesRead += bytesRead;

            if (state.bytesRead == state.bytesToRead)
            {
                byte[] data = new byte[state.bytes.Length];
                Buffer.BlockCopy(state.bytes, 0, data, 0, data.Length);
                Message m = MessageHandler.getMessage(data);
                state.bytesToRead = -1;
                state.bytesRead = 0;
                state.buffer = new byte[StateObject.BufferSize];
                state.bytes = null;
                messages.Enqueue(m);
            }
        }

        if (state.loggedIn)
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, ReadCallback, state);
    }

    protected void Send(Socket client, string data, byte[] bytes = null)
    {
        byte[] ret = MessageHandler.getByteMessage(data, bytes);

        client.BeginSend(ret, 0, ret.Length, 0,
            SendCallback, client);
    }

    protected void SendCallback(IAsyncResult ar)
    {
        try
        {
            Socket handler = (Socket)ar.AsyncState;

            int bytesSent = handler.EndSend(ar);
            //Console.WriteLine("Sent {0} bytes to client.", bytesSent);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    public virtual void HandleMessages()
    {
        
    }
}

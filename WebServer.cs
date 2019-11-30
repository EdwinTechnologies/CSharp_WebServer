using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;


public class StateObject
{
    // Client  socket.
    public Socket workSocket = null;
    // Size of receive buffer.
    public const int BufferSize = 1024;
    // Receive buffer.
    public byte[] buffer = new byte[BufferSize];
    // Received data string.
    public StringBuilder sb = new StringBuilder();
}

public class AsynchronousSocketListener
{
    public static ManualResetEvent allDone = new ManualResetEvent(false);


    public AsynchronousSocketListener()
    {
    }



    public static void StartListening()
    {
        byte[] bytes = new Byte[1024];

        IPAddress ipAddress = IPAddress.Parse("0.0.0.0");
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 80);

        Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            listener.Bind(localEndPoint);
            listener.Listen(500);

            while (true)
            {
                allDone.Reset();

                //Console.WriteLine("Waiting for a connection...\n");
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                allDone.WaitOne();
            }

        }
        catch (Exception e)
        {
            //Nothing
        }

        //Console.Read();

    }




    ////////////////////////////////////////////

    public static string HTTP_Respose = "HTTP/1.1 200 OK";
    public static string Date = "Date: Wed, 10 Oct 2018 15:00:00 GMT";
    public static string Server = "Server: Simple Webserver .NET/C# [EdwinTechnologies]";
    public static string Last_Modified = "Last-Modified: Wed, 10 Oct 2018 15:00:00 GMT";
    public static string Accept_Ranges = "Accept-Ranges: bytes";
    public static string Content_Length = "?";
    public static string Connection = "Connection: close";
    public static string Content_Type = "Content-Type: text/html";

    //public static string CONTENT = "";


    ////////////////////////////////////////////

    public static string getBetween(string strSource, string strStart, string strEnd)
    {

        try
        {
            int Start, End;
            if (strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                End = strSource.IndexOf(strEnd, Start);
                return strSource.Substring(Start, End - Start);
            }
            else
            {
                return "";
            }
        }
        catch (Exception e)
        {
            return "";
        }
    }


    public static void AcceptCallback(IAsyncResult ar)
    {
        // Signal the main thread to continue.
        allDone.Set();

        // Get the socket that handles the client request.
        Socket listener = (Socket)ar.AsyncState;
        Socket handler = listener.EndAccept(ar);

        // Create the state object.
        StateObject state = new StateObject();
        state.workSocket = handler;

        state.buffer = new byte[1024];

        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);       //Get Request Header





        
    }

    public static void ReadCallback(IAsyncResult ar)
    {
        try
        {
            String content = String.Empty;

            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.workSocket;

            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                content = state.sb.ToString();

                string RequestedPath = getBetween(content, "GET ", "HTTP");

                //Console.WriteLine("\nRequest Header: \n" + content);
                Console.WriteLine("Requested Path: " + "localhost" +  RequestedPath);


                try
                {
                    if (RequestedPath == "/ ")
                    {
                        string CONTENT = File.ReadAllText("index.html");
                        Send(handler, Encoding.ASCII.GetBytes(HTTP_Respose + "\n" + Date + "\n" + Server + "\n" + Last_Modified + "\n" + Accept_Ranges + "\n" + "Content-Length: " + CONTENT.Length.ToString() + "\n" + Connection + "\n" + Content_Type + "\n\n" + CONTENT));
                    }
                    else if (getBetween(RequestedPath, ".", " ") == "png")
                    {
                        Send(handler, Encoding.ASCII.GetBytes(HTTP_Respose + "\n" + "Content-Length: " + File.ReadAllBytes(Directory.GetCurrentDirectory() + RequestedPath).Length.ToString() + "\n" + "Content-Type: image/png" + "\n\n"));
                        Send(handler, File.ReadAllBytes(Directory.GetCurrentDirectory() + RequestedPath));
                    }
                    else if (getBetween(RequestedPath, ".", " ") == "jpg")
                    {
                        Send(handler, Encoding.ASCII.GetBytes(HTTP_Respose + "\n" + "Content-Length: " + File.ReadAllBytes(Directory.GetCurrentDirectory() + RequestedPath).Length.ToString() + "\n" + "Content-Type: image/jpg" + "\n\n"));
                        Send(handler, File.ReadAllBytes(Directory.GetCurrentDirectory() + RequestedPath));
                    }
                    else if (getBetween(RequestedPath, ".", " ") == "jpeg")
                    {
                        Send(handler, Encoding.ASCII.GetBytes(HTTP_Respose + "\n" + "Content-Length: " + File.ReadAllBytes(Directory.GetCurrentDirectory() + RequestedPath).Length.ToString() + "\n" + "Content-Type: image/jpeg" + "\n\n"));
                        Send(handler, File.ReadAllBytes(Directory.GetCurrentDirectory() + RequestedPath));
                    }
                    else if (getBetween(RequestedPath, ".", " ") == "ttf")
                    {
                        Send(handler, Encoding.ASCII.GetBytes(HTTP_Respose + "\n" + "Content-Length: " + File.ReadAllBytes(Directory.GetCurrentDirectory() + RequestedPath).Length.ToString() + "\n" + "Content-Type: font/ttf" + "\n\n"));
                        Send(handler, File.ReadAllBytes(Directory.GetCurrentDirectory() + RequestedPath));
                    }
                    else
                    {
                        string CONTENT = File.ReadAllText(Directory.GetCurrentDirectory() + RequestedPath);
                        Send(handler, Encoding.ASCII.GetBytes(HTTP_Respose + "\n" + Date + "\n" + Server + "\n" + Last_Modified + "\n" + Accept_Ranges + "\n" + "Content-Length: " + CONTENT.Length.ToString() + "\n" + Connection + "\n" + Content_Type + "\n\n" + CONTENT));
                    }


                }
                catch (Exception)
                {
                    string CONTENT = "ERROR 404";
                    Send(handler, Encoding.ASCII.GetBytes(HTTP_Respose + "\n" + Date + "\n" + Server + "\n" + Last_Modified + "\n" + Accept_Ranges + "\n" + "Content-Length: " + CONTENT.Length.ToString() + "\n" + Connection + "\n" + Content_Type + "\n\n" + CONTENT));
                }

                

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
        }
        catch (Exception e)
        {

        }

    }

    private static void Send(Socket handler, byte[] data)
    {
        try
        {
            handler.BeginSend(data, 0, data.Length, 0, new AsyncCallback(SendCallback), handler);
        }
        catch (Exception)
        {
            StartListening();
        }
    }

    private static void SendCallback(IAsyncResult ar)
    {
        try
        {
            Socket handler = (Socket)ar.AsyncState;

            int bytesSent = handler.EndSend(ar);
            //Console.WriteLine("Sent {0} bytes to client.", bytesSent);
        }
        catch (Exception e)
        {
            //Nothing
        }
    }

    public static int Main(String[] args)
    {
        StartListening();
        return 0;
    }
}
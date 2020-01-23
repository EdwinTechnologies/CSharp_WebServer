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


    ////////////////////////////////////////////

    public static int ListenPort = 80;
    public static string IndexFile = "index.html";

    public static string HTTP_Respose = "HTTP/1.1 200 OK";
    public static string Date = "Date: Mon, 20 Jan 2020 15:00:00 GMT";
    public static string Server = "Server: Simple Webserver .NET/C# [https://edwintech.ddns.net/]";
    public static string Last_Modified = "Last-Modified: Mon, 20 Jan 2020 15:00:00 GMT";
    public static string Accept_Ranges = "Accept-Ranges: bytes";
    public static string Content_Length = "?";
    public static string Connection = "Connection: close";

    ////////////////////////////////////////////

    public static ManualResetEvent allDone = new ManualResetEvent(false);


    public AsynchronousSocketListener()
    {

    }

    public static void StartListening()
    {
        byte[] bytes = new Byte[1024];

        IPAddress ipAddress = IPAddress.Parse("0.0.0.0");
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, ListenPort);

        Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        try
        {
            
            if (!File.Exists(IndexFile))
            {
                Log("WARNING: " + IndexFile + " not found!");
            }

            Log("Binding...");

            listener.Bind(localEndPoint);
            listener.Listen(999);

            Log("Listening on *:" + ListenPort);

            while (true)
            {
                allDone.Reset();

                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

                allDone.WaitOne();
            }

        }
        catch (Exception)
        {
            //Console.WriteLine("Connection Error");
        }

        //Console.Read();

    }


    public static void Log(string value)
    {
        Console.WriteLine("[" + DateTime.Now.ToString("h:mm:ss") + "] " + value);
    }


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
        allDone.Set();

        Socket listener = (Socket)ar.AsyncState;
        Socket handler = listener.EndAccept(ar);

        StateObject state = new StateObject();
        state.workSocket = handler;

        state.buffer = new byte[1024];

        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);       //Get Request Header
    }

    public static string Reverse(string s)
    {
        char[] charArray = s.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
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

                string RequestedPath = getBetween(content, "GET ", " HTTP");

                RequestedPath = RequestedPath.Replace("%20", " ");
                RequestedPath = RequestedPath.Replace("%7B", "{");
                RequestedPath = RequestedPath.Replace("%7D", "}");

                string FileType = Reverse(getBetween(Reverse(RequestedPath + "*"), "*", "."));

                byte[] FileContent;

                //Log("\nRequest Header: \n" + content);

                try
                {
                    if (RequestedPath == "/")
                    {
                        if (File.Exists(Directory.GetCurrentDirectory() + RequestedPath + "/" + IndexFile))
                        {

                            if (Reverse(RequestedPath)[0] != '/')
                            {
                                FileContent = Encoding.ASCII.GetBytes("<!DOCTYPE html><html><head><title>Redirecting...</title><meta http-equiv = \"refresh\" content = \"2; url = " + RequestedPath + "/" + "\" /></head><body><p>Redirecting...</p></body></html>"); ;
                            }
                            else
                            {
                                FileContent = File.ReadAllBytes(Directory.GetCurrentDirectory() + RequestedPath + "/" + IndexFile);
                            }
                        }
                        else
                        {
                            string DirContent = "";
                            DirectoryInfo Dir = new DirectoryInfo(Directory.GetCurrentDirectory() + RequestedPath);

                            DirContent = "<!DOCTYPE html>\n<html>\n<body>\n<table style=\"width: 100 % \">\n" + "<tr>\n" + "<th>NAME</th>\n<th>TYPE</th>\n</tr>\n";

                            foreach (FileInfo file in Dir.GetFiles())
                            {
                                DirContent += "<tr>\n";
                                DirContent += "<td>" + " <a href=\"" + RequestedPath + file.Name + "\">" + file.Name + "</a> " + "</td>\n";
                                DirContent += "<td>&lt;FILE&gt;</td>\n";
                                DirContent += "</tr>\n";
                            }

                            foreach (DirectoryInfo dir in Dir.GetDirectories())
                            {
                                DirContent += "<tr>\n";
                                DirContent += "<td>" + " <a href=\"" + RequestedPath + dir.Name + "\">" + dir.Name + "</a> " + "</td>\n";
                                DirContent += "<td>&lt;DIR&gt;</td>\n";
                                DirContent += "</tr>\n";
                            }

                            DirContent += "</table>\n</body>\n</html>";

                            FileContent = Encoding.UTF8.GetBytes(DirContent);
                        }



                        SendHeader(handler, FileContent, "text/html");
                        Send(handler, FileContent);
                        //FileContent = File.ReadAllBytes("index.html");

                        //SendHeader(handler, FileContent, "text/html");
                        //Send(handler, FileContent);
                    }
                    else if (FileType == "")
                    {
                        if (File.Exists(Directory.GetCurrentDirectory() + RequestedPath + "/" + IndexFile))
                        {

                            if (Reverse(RequestedPath)[0] != '/')
                            {
                                FileContent = Encoding.ASCII.GetBytes("<!DOCTYPE html><html><head><title>Redirecting...</title><meta http-equiv = \"refresh\" content = \"2; url = " + RequestedPath + "/" + "\" /></head><body><p>Redirecting...</p></body></html>"); ;
                            }
                            else
                            {
                                FileContent = File.ReadAllBytes(Directory.GetCurrentDirectory() + RequestedPath + "/" + IndexFile);
                            }
                        }
                        else
                        {
                            string DirContent = "";
                            DirectoryInfo Dir = new DirectoryInfo(Directory.GetCurrentDirectory() + RequestedPath);

                            DirContent = "<!DOCTYPE html>\n<html>\n<body>\n<table style=\"width: 100 % \">\n" + "<tr>\n" + "<th>NAME</th>\n<th>TYPE</th>\n</tr>\n";

                            foreach (FileInfo file in Dir.GetFiles())
                            {
                                DirContent += "<tr>\n";
                                DirContent += "<td>" + " <a href=\"" + RequestedPath + "/" + file.Name + "\">" + file.Name + "</a> " + "</td>\n";
                                DirContent += "<td>&lt;FILE&gt;</td>\n";
                                DirContent += "</tr>\n";
                            }

                            foreach (DirectoryInfo dir in Dir.GetDirectories())
                            {
                                DirContent += "<tr>\n";
                                DirContent += "<td>" + " <a href=\"" + RequestedPath + "/" + dir.Name + "\">" + dir.Name + "</a> " + "</td>\n";
                                DirContent += "<td>&lt;DIR&gt;</td>\n";
                                DirContent += "</tr>\n";
                            }

                            DirContent += "</table>\n</body>\n</html>";

                            FileContent = Encoding.UTF8.GetBytes(DirContent);
                        }

                        

                        SendHeader(handler, FileContent, "text/html");
                        Send(handler, FileContent);
                    }
                    else if (FileType == "html")
                    {
                        FileContent = File.ReadAllBytes(Directory.GetCurrentDirectory() + RequestedPath);

                        SendHeader(handler, FileContent, "text/html");
                        Send(handler, FileContent);
                    }
                    else if (FileType == "htm")
                    {
                        FileContent = File.ReadAllBytes(Directory.GetCurrentDirectory() + RequestedPath);

                        SendHeader(handler, FileContent, "text/html");
                        Send(handler, FileContent);
                    }
                    else if (FileType == "txt")
                    {
                        FileContent = File.ReadAllBytes(Directory.GetCurrentDirectory() + RequestedPath);

                        SendHeader(handler, FileContent, "text/plain");
                        Send(handler, FileContent);
                    }
                    else if (FileType == "png")
                    {
                        FileContent = File.ReadAllBytes(Directory.GetCurrentDirectory() + RequestedPath);

                        SendHeader(handler, FileContent, "image/png");
                        Send(handler, FileContent);
                    }
                    else if (FileType == "jpg")
                    {
                        FileContent = File.ReadAllBytes(Directory.GetCurrentDirectory() + RequestedPath);

                        SendHeader(handler, FileContent, "image/jpg");
                        Send(handler, FileContent);
                    }
                    else if (FileType == "jpeg")
                    {
                        FileContent = File.ReadAllBytes(Directory.GetCurrentDirectory() + RequestedPath);

                        SendHeader(handler, FileContent, "image/jpeg");
                        Send(handler, FileContent);
                    }
                    else if (FileType == "ttf")
                    {
                        FileContent = File.ReadAllBytes(Directory.GetCurrentDirectory() + RequestedPath);

                        SendHeader(handler, FileContent, "font/ttf");
                        Send(handler, FileContent);
                    }
                    else if (FileType == "js")
                    {
                        FileContent = File.ReadAllBytes(Directory.GetCurrentDirectory() + RequestedPath);

                        SendHeader(handler, FileContent, "text/javascript");
                        Send(handler, FileContent);
                    }
                    else if (FileType == "css")
                    {
                        FileContent = File.ReadAllBytes(Directory.GetCurrentDirectory() + RequestedPath);

                        SendHeader(handler, FileContent, "text/css");
                        Send(handler, FileContent);
                    }
                    else
                    {
                        FileContent = File.ReadAllBytes(Directory.GetCurrentDirectory() + RequestedPath);

                        SendHeader(handler, FileContent, "unknown-type");
                        Send(handler, FileContent);
                    }

                    Log("Requested Path: " + RequestedPath + " | " + "FileType: '" + FileType + "'");

                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();

                }
                catch (Exception)
                {
                    if (File.Exists("404.html"))
                    {
                        FileContent = File.ReadAllBytes("404.html");
                    }
                    else
                    {
                        FileContent = Encoding.ASCII.GetBytes("ERROR 404");
                    }

                    Send(handler, Encoding.ASCII.GetBytes("HTTP/1.1 404 Not Found" + "\n" + Date + "\n" + Server + "\n" + Last_Modified + "\n" + Accept_Ranges + "\n" + "Content-Length: " + FileContent.Length.ToString() + "\n" + Connection + "\n" + "Content-Type: " + "text/html" + "\n\n"));

                    Send(handler, FileContent);

                    Log("ERROR 404: Requested Path: " + RequestedPath + " | " + "FileType: '" + FileType + "'");

                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }

            }
        }
        catch (Exception e)
        {
            Log(e.ToString());
        }
    }


    private static void SendHeader(Socket handler, byte[] FileContent, string ContentType)
    {
        Send(handler, Encoding.ASCII.GetBytes(HTTP_Respose + "\n" + Date + "\n" + Server + "\n" + Last_Modified + "\n" + Accept_Ranges + "\n" + "Content-Length: " + FileContent.Length.ToString() + "\n" + Connection + "\n" + "Content-Type: "+ ContentType + "\n\n"));
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
        }
        catch (Exception)
        {
            //Console.WriteLine(e);
        }
    }

    public static int Main(String[] args)
    {
        StartListening();
        return 0;
    }
}
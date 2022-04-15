using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HttpBPServer
{


    class HttpServer
    {
        public static HttpListener listener;
        public static string url = "http://localhost:33322/";
        public static int pageViews = 0;
        public static int requestCount = 0;
        public static string pageData = "OK";

        static byte[] lastdata;

        public static async Task HandleIncomingConnections()
        {
            bool runServer = true;

            // While a user hasn't visited the `shutdown` url, keep on handling requests
            while (runServer)
            {
                try { 
                    // Will wait here until we hear from a connection
                    HttpListenerContext ctx = await listener.GetContextAsync();

                    // Peel out the requests and response objects
                    HttpListenerRequest req = ctx.Request;
                    HttpListenerResponse resp = ctx.Response;

                    // Print out some info about the request
                    Console.WriteLine("Request #: {0}", ++requestCount);
                    Console.WriteLine(req.Url.ToString());
                    Console.WriteLine(req.HttpMethod);
                    Console.WriteLine(req.UserHostName);
                    Console.WriteLine(req.UserAgent);
                    Console.WriteLine();


                    // Make sure we don't increment the page views counter if `favicon.ico` is requested
                    if (req.Url.AbsolutePath != "/favicon.ico")
                        pageViews += 1;


                    // If `shutdown` url requested w/ POST, then shutdown the server after serving the page
                    if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/shutdown"))
                    {
                        Console.WriteLine("Shutdown requested");
                        runServer = false;
                    }


                    if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/send"))
                    {
                        Console.WriteLine("OK /send.");

                        // Write the response info
                        string disableSubmit = !runServer ? "disabled" : "";
                        lastdata = ReadFully(req.InputStream);
                        resp.ContentType = "text/html";
                        resp.ContentEncoding = Encoding.UTF8;
                        resp.ContentLength64 = lastdata.LongLength;

                        // Write out to the response stream (asynchronously), then close it
                        await resp.OutputStream.WriteAsync(lastdata, 0, lastdata.Length);
                        resp.Close();
                    }


                    else if ((req.HttpMethod == "POST") && (req.Url.AbsolutePath == "/receive") && lastdata != null)
                    {
                        Console.WriteLine("OK /receive.");

                        // Write the response info
                        string disableSubmit = !runServer ? "disabled" : "";
                        byte[] data = lastdata;
                        resp.ContentType = "text/html";
                        resp.ContentEncoding = Encoding.UTF8;
                        resp.ContentLength64 = data.LongLength;

                        // Write out to the response stream (asynchronously), then close it
                        await resp.OutputStream.WriteAsync(data, 0, data.Length);
                        resp.Close();
                    }


                    else { 
                
                        // Write the response info
                        string disableSubmit = !runServer ? "disabled" : "";
                        byte[] data = Encoding.UTF8.GetBytes(String.Format(pageData, pageViews, disableSubmit));
                        resp.ContentType = "text/html";
                        resp.ContentEncoding = Encoding.UTF8;
                        resp.ContentLength64 = data.LongLength;

                        // Write out to the response stream (asynchronously), then close it
                        await resp.OutputStream.WriteAsync(data, 0, data.Length);
                        resp.Close();

                    }
                }
                catch (Exception ex) { Console.WriteLine(ex.ToString()); }
            }
        }


        public static void Main(string[] args)
        {
            // Create a Http server and start listening for incoming connections
            listener = new HttpListener();
            listener.Prefixes.Add(url);
            listener.Start();
            Console.WriteLine("Listening for connections on {0}", url);

            // Handle requests
            Task listenTask = HandleIncomingConnections();
            listenTask.GetAwaiter().GetResult();

            // Close the listener
            listener.Close();
        }


        public static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }

}

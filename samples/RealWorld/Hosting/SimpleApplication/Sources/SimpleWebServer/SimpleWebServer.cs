// To test outside Service Fabric: 
// From Admin CMD: netsh http add urlacl url=http://*:8080/ user=MicrosoftAccount\JeffRichter@live.com listen=yes

using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWebServer {
   internal static class SimpleWebServer {
      private const String c_br = "<br/>", c_nbsp = "&nbsp;";
      private static readonly string s_nodeId = Environment.GetEnvironmentVariable("Fabric_NodeId") ?? "Not running on Service Fabric";
      //private static readonly String m_webPage = File.ReadAllText("WebPage.htm");
      //private static readonly String s_ServerIP = /*RoleEnvironment.IsEmulated ? "localhost" : */"RoleFeatures.cloudapp.net";

      private static void Main(string[] args) {
         Int32 port = Int32.Parse(args[0]);
         var exs = (from type in typeof(Exception).Assembly.DefinedTypes
                   where typeof(Exception).IsAssignableFrom(type)
                   select type.FullName).ToArray();
         var s = String.Join(Environment.NewLine, exs);
         HttpListener m_listener = new HttpListener();
         m_listener.Prefixes.Add($"http://+:{port}/");

         //m_logPathname = Path.Combine(tempStorage, m_roleInstance.Id + ".txt");
         Log("Start");

         m_listener.Start();  // Requires Admin privileges

         while (true) {
            HttpListenerContext context = m_listener.GetContext();   // Wait for client request
            String requestUri = context.Request.Url.ToString();      // Sometimes we get requests for \favicon.ico, See http://en.wikipedia.org/wiki/Favicon
            if (requestUri.Contains("favicon.ico")) continue;
            Log(requestUri);
            ProcessRequestAsync(context).ContinueWith(t => t.Exception, TaskContinuationOptions.OnlyOnFaulted); // Swallow exception
         }
      }
      private static async Task ProcessRequestAsync(HttpListenerContext context) {
         String plainText = $"Node Id: {s_nodeId}"; 
         /*
         switch (context.Request.QueryString["cmd"]) {
            default:
            case "instance":
               plainText = 
               break;
         }*/
         using (HttpListenerResponse response = context.Response) {
            String html = plainText ?? "" /*?? m_webPage
                  .Replace("InstanceId", DateTime.UtcNow.ToLongTimeString() + ": " + machineName)
                     .Replace("ServerIP", s_ServerIP).Replace("HtmlText", htmlText)*/;

            Byte[] responseData = Encoding.UTF8.GetBytes(html);
            context.Response.ContentLength64 = responseData.Length;
            await context.Response.OutputStream.WriteAsync(responseData, 0, responseData.Length);
         }
      }

      private static void Log(String format, params Object[] args) {
//         File.AppendAllText(m_logPathname, DateTime.UtcNow.ToString("u")
  //          + ": " + String.Format(format, args) + Environment.NewLine);
      }
   }
}

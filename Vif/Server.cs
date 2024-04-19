using System.Net;
using System.Net.Sockets;
using WatsonWebserver.Core;

namespace Vif_siemens_compiler.Vif;
using WatsonWebserver;


public class Server
{
    public static TaskCompletionSource<string> ok = new TaskCompletionSource<string>();
    public int port;

    public Server()
    {
        var server = new Webserver(new WebserverSettings("127.0.0.1", FreeTcpPort()), DefaultRoute);
        server.Start();
        port = server.Settings.Port;
        server.Settings.Headers.DefaultHeaders["Cross-Origin-Resource-Policy"] = "cross-origin";
    }

    private static Task DefaultRoute(HttpContextBase ctx)
    {
        var reader = new StreamReader(ctx.Request.Data);
        var requestFromPost = reader.ReadToEnd();
        ctx.Response.Send();

        ok.TrySetResult(requestFromPost);
        return Task.CompletedTask;
    }
    
    static int FreeTcpPort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        var port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }
}
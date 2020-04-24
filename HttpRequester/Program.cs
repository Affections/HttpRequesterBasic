namespace HttpRequester
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    class Program
    {
        static Dictionary<string, int> SessionStore = new Dictionary<string, int>();
        static async Task Main(string[] args)
        {
            TcpListener tcpListener = new TcpListener(IPAddress.Loopback,80);
            tcpListener.Start();
            while (true)
            {
                TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();
                Task.Run(() => ProcessClientAsync(tcpClient));
            }
        }

        private static async Task ProcessClientAsync(TcpClient tcpClient)
        {
            const string NewLine = "\r\n";
            using NetworkStream networkStream = tcpClient.GetStream();
            byte[] requestBytes = new byte[1000000]; // TODO: use buffer
            int bytesRead = await networkStream.ReadAsync(requestBytes, 0, requestBytes.Length);
            string request = Encoding.UTF8.GetString(requestBytes, 0, bytesRead);

            var sid = Regex.Match(request, @"sid=[^\n]*\n").Value?.Replace("sid=", string.Empty).Trim();
            Console.WriteLine(sid);
            var newSid = Guid.NewGuid().ToString();
            var count = 0;
            if(SessionStore.ContainsKey(sid))
            {
                SessionStore[sid]++;
                count = SessionStore[sid];
            }
            else
            {
                sid = null;
                SessionStore[newSid] = 1;
                count = 1;
            }
            string responseText = @"<form action='/Account/Login' method='post'> 
<input type=date name='date' />
<input type=text name='username' />
<input type=password name='password' />
<input type=submit value='Login' />
</form>";
            string response = "HTTP/1.0 200 OK" + NewLine +
                "Server: BlagoServer/1.0" + NewLine +
                "Content-Type: text/html" + NewLine +
                "Set-Cookie: user=Blago; Max-Age: 3600; HttpOnly;" + NewLine +
                (string.IsNullOrWhiteSpace(sid) ?
                ("Set-Cookie: sid=" + newSid + NewLine) : String.Empty) +
                "Content-Length: " + responseText.Length + NewLine + NewLine + responseText;
            byte[] responseBytes = Encoding.UTF8.GetBytes(response);
            await networkStream.WriteAsync(responseBytes, 0, responseBytes.Length);
            Console.WriteLine(request);
            Console.WriteLine(new string('=', 60));


        }
    }
}

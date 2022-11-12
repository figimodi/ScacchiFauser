using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ChessServer
{
    public static class AsynchronousSocketListener
    {
        private const int CLIENT = 10; // identifica quanti client possono richiedere l'apertura di una connessione
        private static byte[] _buffer = new byte[1024];
        private static PlayersHandler _ph = new PlayersHandler(CLIENT); // Istanza della classe PlayersHandler, la quale gestisce i messaggi in entrata e formula una risposta
        private static Socket _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        static readonly string _fileCertificatoServer = "self-server.pfx";
        static readonly string _passwordCertificatoServer = null;

        public static void SetupServer()
        {
            Console.WriteLine("Inzializzazione server...");
            _serverSocket.Bind(new IPEndPoint(IPAddress.Any, 70));
            _serverSocket.Listen(CLIENT);

            Console.WriteLine("Server in ascolto...");
            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallBack), null);
        }

        private static void AcceptCallBack(IAsyncResult AR)
        {
            Socket socket = _serverSocket.EndAccept(AR);
            SslStream stream;

            try {     
                var cert = new X509Certificate2(_fileCertificatoServer, _passwordCertificatoServer);
                stream = new SslStream(new NetworkStream(socket), false);                
                stream.AuthenticateAsServer(cert, false, SslProtocols.Tls12, false);

                ConsoleColor appo = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Autenticazione completata");
                Console.ForegroundColor = appo;
                Console.WriteLine(socket.RemoteEndPoint.ToString() + " connesso al server");

                stream.BeginRead(_buffer, 0, _buffer.Length, new AsyncCallback(ReceiveCallBack), stream);
            }
            catch (Exception e) {
                Console.WriteLine("Errore del server: {0}", e.Message);
            }

            _serverSocket.BeginAccept(new AsyncCallback(AcceptCallBack), null);
        }

        private static void ReceiveCallBack(IAsyncResult AR)
        {
            try
            {
                SslStream stream = (SslStream)AR.AsyncState;
                int received = stream.EndRead(AR);

                // Testo ricevuto
                string textRx = Encoding.UTF8.GetString(_buffer, 0, received);

                // Viene generata una risposta testuale textTx che viene generata tramite la calsse PlayersHandler con il metodo ProcessData
                // Notare che viene passato come parametro anche il socket poichè la classe PlayersHandler gestisce anche l'online (vedere la classe PlayersHandler)
                string textTx = _ph.ProcessData(textRx, stream);

                if (textTx != String.Empty) {
                    byte[] dataTx = Encoding.UTF8.GetBytes(textTx);
                    stream.BeginWrite(dataTx, 0, dataTx.Length, new AsyncCallback(SendCallBack), stream);
                }

                stream.BeginRead(_buffer, 0, _buffer.Length, new AsyncCallback(ReceiveCallBack), stream);
            }
            catch {
                SslStream stream = (SslStream)AR.AsyncState;

                // Annuncia all'istanza _ph della classe PlayersHandler la disconnessione dell'utente
                _ph.ProcessData("OUT/ConnectionClosed", stream);
                stream.Close();                
            }
        }

        private static void SendCallBack(IAsyncResult AR)
        {
            try
            {
                SslStream stream = (SslStream)AR.AsyncState;
                stream.EndWrite(AR);
            }
            catch {
                SslStream stream = (SslStream)AR.AsyncState;

                // Annuncia all'istanza _ph della classe PlayersHandler la disconnessione dell'utente
                _ph.ProcessData("OUT/ConnectionClosed", stream);
                stream.Close();
            }
        }  
    }
}

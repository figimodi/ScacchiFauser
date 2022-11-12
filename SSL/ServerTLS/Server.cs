using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

/*
 *         Progetto: Applicazione Client/Server basata su SSL/TLS
 * 
 *           Autore: Roberto Fuligni
 *  Ultima modifica: 24/03/2018
 *  
 *      Descrizione: Modulo server, comunicazione mediante SslStream
 *                   e certificati digitali autofirmati.
 *                   
 *                   Per generare un certificato autofirmato per il server,
 *                   aprire il prompt dei comandi degli sviluppatori di Visual
 *                   Studio, quindi eseguire i seguenti comandi:
 *                    
 *                   rem Creazione di chiave privata e certificato autofirmato
 *                   rem (1.3.6.1.5.5.7.3.1 = ServerAuth)
 *                   makecert -r -pe -n "CN=Server" -a sha256 -sky signature -cy end -sv self-server.pvk -len 2048 -m 70 -eku 1.3.6.1.5.5.7.3.1 self-server.cer
 *                   
 *                   rem Incapsula chiave e certificato in un file .pfx
 *                   pvk2pfx /pvk self-server.pvk /spc self-server.cer /pfx self-server.pfx
 *                   
 *                   rem Copiare "self-server.pfx" nella cartella contenente il file eseguibile del server
 */

namespace ServerTLS
{
    class Server
    {

        static readonly string FileCertificatoServer = "self-server.pfx";
        static readonly string PasswordCertificatoServer = null;

        static readonly int PortaServer = 5000;

        static void Invia(SslStream s, string msg)
        {
            var buffer = Encoding.UTF8.GetBytes(msg);
            s.Write(buffer);
        }

        static string Ricevi(SslStream s)
        {
            var buffer = new byte[8192];
            var letti = 0;
            while (letti == 0)
            {
                letti = s.Read(buffer, 0, buffer.Length);
            }
            var ris = Encoding.UTF8.GetString(buffer, 0, letti);
            return ris;
        }

        static void StampaColore(string msg, ConsoleColor colore)
        {
            ConsoleColor backup = Console.ForegroundColor;
            Console.ForegroundColor = colore;
            Console.WriteLine(msg);
            Console.ForegroundColor = backup;
        }

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Modulo SERVER");
                Console.WriteLine("=====================================");
                var cert = new X509Certificate2(FileCertificatoServer, PasswordCertificatoServer);

                var listener = new TcpListener(IPAddress.Any, PortaServer);

                listener.Start();
                Console.WriteLine("Server in ascolto sulla porta {0}.", PortaServer);
                Console.WriteLine("Premere CTRL-C per terminare.");

                while (true)
                {
                    using (var client = listener.AcceptTcpClient())
                    using (var stream = new SslStream(client.GetStream(), false, ValidaCertificato))
                    {
                        Console.WriteLine();
                        Console.WriteLine(String.Format("Client {0} connesso.", client.Client.RemoteEndPoint));

                        stream.AuthenticateAsServer(cert, true, SslProtocols.Tls12, false);
                        StampaColore("Autenticazione completata.", ConsoleColor.Green);
                        Console.WriteLine("Certificato locale: {0}", stream.LocalCertificate.Subject);
                        Console.WriteLine("Certificato remoto: {0}", stream.RemoteCertificate.Subject);
                        Console.WriteLine();

                        var msg = "Buongiorno client!";
                        Invia(stream, msg);
                        Console.WriteLine("Messaggio inviato: {0}", msg);

                        msg = Ricevi(stream);
                        Console.WriteLine("Messaggio ricevuto: {0}", msg);
                    }
                }
            }
            catch (Exception ex)
            {
                StampaColore(String.Format("Errore del server: {0}", ex.Message), ConsoleColor.Red);
            }
        }


        private static bool ValidaCertificato(Object sender, X509Certificate certificato, X509Chain catena, SslPolicyErrors errori)
        {
            if (errori == SslPolicyErrors.None) {
                return true;
            }

            // I certificati autofirmati non sono autenticati da CA attendibili.
            // Data la finalità didattica di questa applicazione, il client è configurato
            // per accettare i certificati autofirmati presentati dai server. 

            if (errori == SslPolicyErrors.RemoteCertificateChainErrors) {
                return true;
            }

            Console.Error.WriteLine("Errore di validazione: " + errori.ToString());
            return false;
        }
    }
}

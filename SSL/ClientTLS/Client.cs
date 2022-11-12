using System;
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
 *      Descrizione: Modulo client, comunicazione mediante SslStream
 *                   e certificati digitali autofirmati.
 *                   
 *                   Per generare un certificato autofirmato per il client,
 *                   aprire il prompt dei comandi degli sviluppatori di Visual
 *                   Studio, quindi eseguire i seguenti comandi:
 *                    
 *                   rem Creazione di chiave privata e certificato autofirmato
 *                   rem (1.3.6.1.5.5.7.3.2 = ClientAuth)
 *                   makecert -r -pe -n "CN=Utente" -a sha256 -sky signature -cy end -sv self-utente.pvk -len 2048 -m 70 -eku 1.3.6.1.5.5.7.3.2 self-utente.cer
 *                   
 *                   rem Incapsula chiave e certificato in un file .pfx
 *                   pvk2pfx /pvk self-utente.pvk /spc self-utente.cer /pfx self-utente.pfx
 *                   
 *                   rem Copiare "self-utente.pfx" nella cartella contenente il file eseguibile del client
 */

namespace ClientTLS
{
    class Client
    {
        static readonly string NomeServer = "localhost";
        static readonly int PortaServer = 5000;

        static readonly string NomeCertificatoServer = "Server";
        static readonly string NomeCertificatoClient = "Utente";
        static readonly string FileCertificatoClient = "self-utente.pfx";
        static readonly string PasswordCertificatoClient = null;

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
                Console.WriteLine("Modulo CLIENT");
                Console.WriteLine("=====================================");

                var cert = new X509Certificate2(FileCertificatoClient, PasswordCertificatoClient);
                var collezione = new X509CertificateCollection(new X509Certificate[] { cert });

                using (var client = new TcpClient(NomeServer, PortaServer))
                using (var stream = new SslStream(client.GetStream(), false, ValidaCertificato))
                {
                    Console.WriteLine("Client connesso a {0}:{1}", NomeServer, PortaServer);


                    stream.AuthenticateAsClient(NomeCertificatoServer, collezione, SslProtocols.Tls12, false);

                    StampaColore("Autenticazione completata.", ConsoleColor.Green);
                    Console.WriteLine("Certificato locale: {0}", stream.LocalCertificate.Subject);
                    Console.WriteLine("Certificato remoto: {0}", stream.RemoteCertificate.Subject);
                    Console.WriteLine();

                    var msg = "Salve server!";
                    Invia(stream, msg);
                    Console.WriteLine("Messaggio inviato: {0}", msg);

                    msg = Ricevi(stream);
                    Console.WriteLine("Messaggio ricevuto: {0}", msg);
                }
            }
            catch (Exception ex)
            {
                StampaColore(String.Format("Errore del client: {0}", ex.Message), ConsoleColor.Red);
            }
        }


        private static bool ValidaCertificato(Object sender, X509Certificate certificato, X509Chain catena, SslPolicyErrors errori)
        {
            if (errori == SslPolicyErrors.None)
            {
                return true;
            }

            // I certificati autofirmati non sono autenticati da CA attendibili.
            // Data la finalità didattica di questa applicazione, il server è configurato
            // per accettare i certificati autofirmati presentati dai client. 
 
            if (errori == SslPolicyErrors.RemoteCertificateChainErrors)
            {
                return true;
            }

            Console.Error.WriteLine("Errore di validazione: " + errori.ToString());
            return false;
        }
    }
}

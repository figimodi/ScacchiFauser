using System;

namespace ChessServer
{
    class Server
    {
        public static void Main(String[] args)
        {
            // Controllo che il database esista
            DatabaseScacchi db = new DatabaseScacchi("DataSource=\"db.sdf\"; Password=\"Figimodi2000\"");

            // Avvia un listener che gestisce più socket asincroni
            AsynchronousSocketListener.SetupServer();
            Console.ReadKey();
        }
    }
}

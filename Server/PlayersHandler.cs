using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace ChessServer
{
    public class PlayersHandler
    {
        private int _nConnections; // identifica quanti players possono connettersi
        private DatabaseScacchi _db;
        private Player[] _players;
        private string[] _replay;
        private SslStream _queue;

        private struct Player {
            public SslStream stream;
            public string username;
            public int lobby; // Variabile che indica in quale stanza il player sta giocando (non ancora sviluppata la parte di gioco)
        }

        

        public PlayersHandler(int nConnections) {
            _nConnections = nConnections;

            // Viene creato un array di struct lungo quanto l'intero passato come parametro
            _players = new Player[_nConnections];
            _replay = new string[_nConnections / 2];
            _db = new DatabaseScacchi("DataSource=\"db.sdf\"; Password=\"Figimodi2000\"");

            for (int i = 0; i < _nConnections; i++) {
                _players[i].lobby = 999;
            }
        }

        public string ProcessData(string info, SslStream stream)
        {
            String message = String.Empty;
            String code = String.Empty;

            // Separo il messaggio inziale ogni /, poichè il messaggio avrà la forma XXX/messaggio1/messaggio2/messaggio3
            // Il codice e i messaggi possono essere intesi come campi, fields.
            string[] fields = info.Split('/');

            // Si considera il primo campo (indice 0)
            switch (fields[0])
            {
                // La forma messaggio di Login : LOG/username/password
                case "LOG":
                    code = _db.TryLogIn(fields[1], fields[2], out message);

                    if(code == "LGY/")
                        code = TryToAddPlayer(fields[1], stream, out message);
                    break;

                // La forma messaggio di Registrazione : REG/email/username/password
                case "REG":
                    code = _db.TryRegister(fields[1], fields[2], fields[3], out message);
                    break;

                // Questo messaggio viene ricevuto quando viene cliccato sul link contenuto nell'email,
                // avrà come forma : CON/hash_dell'_email
                // In questo caso message e code saranno Empty
                case "CON":
                    _db.ConfirmRegister(fields[1]);
                    break;

                // Questo messaggio viene esclusivamente mandato internamente dal server per avvisare che un utente si è disconnesso,
                // viene inviato dal server nel senso che quando il server nota che il socket si è chiuso chiama
                // il metodo ProcessData("OUT/") della classe PlayersHandler
                case "OUT":
                    RemovePlayer(stream);
                    break;

                // Messaggio da parte del client il quale ha volontà di giocare alla prima modalità disponibile OM1, ovvero open modalità 1
                case "OM1":
                    Console.WriteLine("Un giocatore in coda");
                    code = QueueMD1(stream, out message);
                    break;

                // CM1, close modalità 1
                case "CM1":
                    _queue = null;
                    Console.WriteLine("Coda libera più giocare");
                    break;
                case "MSG":
                    GestoreMessaggio(stream, Convert.ToInt32(fields[1]), fields[2]);
                    break;
                case "MOV":
                    GestoreMosse(stream, Convert.ToInt32(fields[1]), fields[2]);
                    break;
                case "DSC":
                    // disconnect
                    EndOfMatch(stream, Convert.ToInt32(fields[1]), fields[2], fields[3]);
                    break;
                case "DRP":
                    // draw pieces(mancanza di pezzi) 
                case "DRR":
                    // draw request
                case "DRA":
                    // draw accepted
                case "DRD":
                    // draw declined
                    GestorePareggio(stream, fields[0], Convert.ToInt32(fields[1]));
                    break;
                case "CPS":
                    // cambia password
                    code = _db.TryChangePassword(fields[1], fields[2], out message);
                    break;
                case "PRO":
                    // profiler
                    code = _db.GetUserSettings(fields[1], out message);
                    break;
                case "OPP":
                    // opponent profiler
                    code = _db.GetOpponentSettings(fields[1], out message);
                    break;
                case "CAV":
                    // cambia avatar
                    code = _db.TryChangeAvatar(fields[1], "pack://application:,,,/img/avatar/" + fields[8], out message);
                    break;
                case "AMC":
                    // amici
                    code = _db.GetFriendsInformation(fields[1], out message);
                    message = ElaboraListaAmici(message);
                    break;
                case "SRC":
                    // search (giocatori)
                    code = _db.SearchFriends(fields[1], fields[2], out message);
                    break;
                case "AMR":
                    // amici request
                    code = _db.SearchFriendsRequest(fields[1], out message);
                    break;
                case "CLA":
                    // classifiche
                    code = _db.GetClassifiche(fields[1], out message);
                    break;
                case "SFR":
                    // send friend request
                    code = _db.FriendRequest(fields[1], fields[2], out message);
                    break;
                case "AFR":
                    // accept friend request
                    code = _db.AcceptFriendRequest(fields[1], fields[2], out message);
                    break;
                case "SMR":
                    // send match request
                    CombineFriendsMatch(stream, fields[1], fields[2], fields[3]);
                    break;
                case "REP":
                    code = _db.GetReplayList(fields[1], out message);
                    break;
                case "SHR":
                    code = _db.GetReplay(fields[1], fields[2], fields[3], out message);
                    break;
            }

            // La variabile message viene integrata nel corso del metodo tramite i parametri di out delle funzioni del database
            return (code + message);
        }

        private void GestorePareggio(SslStream stream, string code, int stanza) {

            int index = stanza * 2;
            if (_players[index].stream == stream)
                index++;

            switch (code) {
                case "DRR":
                case "DRD":
                    _players[index].stream.Write(Encoding.UTF8.GetBytes(code));
                    break;
                case "DRP":
                case "DRA":

                    string[] appo = _replay[stanza].Split('/');

                    if (appo[4].Length > 15)
                        _db.PutMatchInReplay(_replay[stanza]);

                    _db.UpdateScore(_players[stanza * 2].username, _players[stanza * 2 + 1].username, true);

                    _replay[stanza] = String.Empty;

                    _players[index].stream.Write(Encoding.UTF8.GetBytes(code));
                    _players[stanza * 2].lobby = 999;
                    _players[stanza * 2 + 1].lobby = 999;
                    break;

            }
        }

        private void EndOfMatch(SslStream stream, int s, string message, string title)
        {
            int index = s * 2;

            if (_players[index].stream == stream)
                index++;

            _players[index].stream.Write(Encoding.UTF8.GetBytes("DSC/" + message + "/" + title));

            string[] appo = _replay[s].Split('/');

            if(appo[4].Length > 15)
                _db.PutMatchInReplay(_replay[s]);

            if (_players[s * 2].stream == stream)
                _db.UpdateScore(_players[s * 2 + 1].username, _players[s * 2].username, false);
            else
                _db.UpdateScore(_players[s * 2].username, _players[s * 2 + 1].username, false);

            _replay[s] = String.Empty;

            _players[s * 2].lobby = 999;
            _players[s * 2 + 1].lobby = 999;
        }

        private void RemovePlayer(SslStream toRemove) {
            int i = 0;
            try
            {
                // Incrementa i fino a trovare il socket corrispondente e il suo indice i
                while (_players[i].stream == null || _players[i].stream != toRemove) i++;

                // Reset dell' array di struct in posizione i
                _players[i].stream = null;
                _players[i].username = String.Empty;
                

                // Se il player è in una stanza esce anche il suo avversario
                if (_players[i].lobby != 999) {

                    _players[i].lobby = 999;

                    if (i % 2 != 0)
                        _players[--i].lobby = 999;
                    else
                        _players[++i].lobby = 999;

                    _players[i].stream.Write(Encoding.UTF8.GetBytes("DSC/L'avversario si è disconnesso, hai vinto la partita/ Hai vinto!"));
                }                
            }
            catch { }
        }

        private string TryToAddPlayer(string username, SslStream stream, out string message) {
            message = String.Empty;
            int i = 0;
            int x = -1; // Assume il valore dell'indice disponibile a memorizzare i dati del giocatore

            // Affinchè l'array non è esaurito i != _nConnections
            while (i != _nConnections) {

                if (_players[i].stream == null && x == -1)
                    x = i; 

                // Verifica che il nome utente dell'account che vuole entrare nel server non sia già contenuto 
                // nell' array, poichè questo significherebbe che sta gia giocando
                if (_players[i].username != null && _players[i].username == username) {
                    message = "Il tuo account sta gia' giocando";
                    return "LGN/";
                }
                i++;
            }

            // Se x = -1 vuole dire che è rimasto invariato, perciò non vi è spazio nel server
            if (x != -1)
            {
                _players[x].stream = stream;
                _players[x].username = username;
                _players[x].lobby = 999;
                return "LGY/";
            }
            else
            {
                message = "Server pieno";
                return "LGN/";
            }
        }

        private string QueueMD1(SslStream stream, out string message) {
            String code = String.Empty;
            message = String.Empty;
            int random = new Random().Next(-2, 0); //random = -2 = bianco
            string username1, username2;

            // Il socket del primo giocatore che vuole giocare viene memorizzato nella variabile _queue
            if (_queue == null)
                _queue = stream;
            else {
                int pos1, pos2;
                username1 = FindNameBySocket(stream, out pos1);
                username2 = FindNameBySocket(_queue, out pos2);

                int stanza = AssegnaStanza(stream, _queue, username1, username2, pos1, pos2);

                if (random == -2)
                    _replay[stanza] = username1 + "/" + username2 + "/" + DateTime.Now.ToString() + "--";
                else
                    _replay[stanza] = username2 + "/" + username1 + "/" + DateTime.Now.ToString() + "--" ;

                code = "FND/";
                message = username1 + "/";
                message += random.ToString() + "/";
                message += stanza.ToString();

                // Se la variabile _queue non è null, vi è un giocatore in coda e di conseguenza
                // viene inviato un pacchetto FND/player found sia al socket _queue e al socket gestito 
                // attualmente dalla classe
                _queue.Write(Encoding.UTF8.GetBytes(code + message));
                Console.WriteLine("Partita iniziata");

                message = username2 + "/";
                if (random == -1) 
                    message += "-2/";
                else message += "-1/";
                message += stanza.ToString();

                // _queue ritorna null per gestire una nuova coda
                _queue = null; 
            }

            return code;
        }

        private void FriendsMatchStart(string username1, string username2)
        {
            String code = String.Empty;
            String message = String.Empty;
            int random = new Random().Next(-2, 0);

            int pos1 = 0, pos2 = 0;
            for(int i = 0; i < _nConnections; i++)
            {
                if (_players[i].username == username1)
                {
                    pos1 = i;
                    break;
                }
                    
            }
            for (int i = 0; i < _nConnections; i++)
            {
                if (_players[i].username == username2)
                {
                    pos2 = i;
                    break;
                }
            }

            int stanza = AssegnaStanza(_players[pos1].stream, _players[pos2].stream, username1, username2, pos1, pos2);

            if (random == -2)
                _replay[stanza] = username1 + "/" + username2 + "/" + DateTime.Now.ToString() + "--";
            else
                _replay[stanza] = username2 + "/" + username1 + "/" + DateTime.Now.ToString() + "--";

            code = "FND/";
            message = username1 + "/";
            message += random.ToString() + "/";
            message += stanza.ToString();

            // Se la variabile _queue non è null, vi è un giocatore in coda e di conseguenza
            // viene inviato un pacchetto FND/player found sia al socket _queue e al socket gestito 
            // attualmente dalla classe
            _players[pos2].stream.Write(Encoding.UTF8.GetBytes(code + message));

            message = username2 + "/";
            if (random == -1)
                message += "-2/";
            else message += "-1/";
            message += stanza.ToString();

            _players[pos1].stream.Write(Encoding.UTF8.GetBytes(code + message));

            Console.WriteLine("Partita amichevole iniziata");
        }

        private string FindNameBySocket(SslStream stream, out int pos) {
            string user = String.Empty;
            pos = 0;

            for (int i = 0; i < _nConnections; i++)
            {
                if (_players[i].stream == stream)
                {
                    user = _players[i].username;
                    pos = i;
                    break;
                }
            }

            return user;
        }

        private int AssegnaStanza(SslStream s1, SslStream s2, string u1, string u2, int x1, int x2)
        {
            int i = 0;
            while (_players[i].lobby != 999) i += 2;
        
            int stanza = i / 2;

            SslStream s_temp;
            string u_temp;
            bool mod1 = true;
            bool mod2 = true;

            if (x1 == i || x1 == i + 1) mod1 = false;
            if (x2 == i || x2 == i + 1) mod2 = false;

            if (mod1)
            {
                // se x2 è gia nella pos1 modifico la pos2
                if (x2 == i) i++;

                s_temp = _players[i].stream;
                u_temp = _players[i].username;
                _players[i].stream = s1;
                _players[i].username = u1;
                _players[x1].stream = s_temp;
                _players[x1].username = u_temp;

                if (mod2)
                {
                    // se è stata modificata la pos2 decremento per modificare pos1
                    if (i % 2 != 0) i--;
                    // se no incremento per modificare la pos2
                    else i++;

                    s_temp = _players[i].stream;
                    u_temp = _players[i].username;
                    _players[i].stream = s2;
                    _players[i].username = u2;
                    _players[x2].stream = s_temp;
                    _players[x2].username = u_temp;
                }
            }
            else
            {
                if (mod2)
                {
                    // se x1 è gia nella pos1 modifico la pos2
                    if (x1 == i) i++;

                    s_temp = _players[i].stream;
                    u_temp = _players[i].username;
                    _players[i].stream = s2;
                    _players[i].username = u2;
                    _players[x2].stream = s_temp;
                    _players[x2].username = u_temp;
                }
            }

            i = stanza * 2;

            _players[i].lobby = stanza;
            _players[i + 1].lobby = stanza;

            return stanza;
        }

        private void GestoreMessaggio(SslStream stream, int s, string testo)
        {
            string message = String.Empty;
            int index = s * 2;
            string username;

            if (_players[index].stream != stream)
                index++;

            username = _players[index].username;
            message += username + "/" + testo;

            if (index % 2 == 0)
                index++;
            else
                index--;

            _players[index].stream.Write(Encoding.UTF8.GetBytes("MSG/" + message));
        }

        private void GestoreMosse(SslStream stream, int s, string mossa)
        {
            int index = s * 2;

            if (_players[index].stream == stream)
                index++;

            _replay[s] += mossa;

            _players[index].stream.Write(Encoding.UTF8.GetBytes("MOV/" + mossa));
        }

        private void CombineFriendsMatch(SslStream stream, string richiedente, string destinatario, string mod)
        {
            for (int ix = 0; ix < _nConnections; ix++)
            {
                if (_players[ix].username == destinatario)
                {
                    if (_players[ix].lobby == 999)
                    {
                        if (mod == "ask")
                            _players[ix].stream.Write(Encoding.UTF8.GetBytes("SMR/ask/" + richiedente));
                        else
                        {
                            if (mod == "yes") FriendsMatchStart(richiedente, destinatario);
                            else _players[ix].stream.Write(Encoding.UTF8.GetBytes("SMR/no"));
                        }
                                  
                    }             
                    else stream.Write(Encoding.UTF8.GetBytes("SMR/error"));

                    break;
                }
                else if (ix == _nConnections - 1) stream.Write(Encoding.UTF8.GetBytes("SMR/error"));
            }
        }

        private string ElaboraListaAmici(string message) {

            string[] fields = message.Split('/');
            List<string> usernameFound = new List<string>();
            int x;
            int index;
            int amici;

            for (int i = 0; i < _nConnections; i++)
            {

                x = 2;
                index = 2;
                amici = Convert.ToInt32(fields[1]); ;

                if (_players[i].username == String.Empty || _players[i].username == null)
                    break;

                for (; x < index + amici; x++)
                {
                    if (fields[x] == _players[i].username)
                    {
                        string addParam = String.Empty;
                        if (fields[x] == _players[i].username)
                        {
                            usernameFound.Add(fields[x]);

                            if (_players[i].lobby != 999)
                                usernameFound.Add("In partita");
                            else
                                usernameFound.Add("Nel menu");
                        }
                    } 
                }
                
                index += amici + 1;
                amici = Convert.ToInt32(fields[2 + amici]);
                x++;
                for (; x < index + amici; x++)
                {
                    if (fields[x] == _players[i].username)
                    {
                        string addParam = String.Empty;
                        if (fields[x] == _players[i].username)
                        {
                            usernameFound.Add(fields[x]);

                            if (_players[i].lobby != 999)
                                usernameFound.Add("In partita");
                            else
                                usernameFound.Add("Nel menu");
                        }
                    }
                }
            }

            x = 2;
            index = 2;
            amici = Convert.ToInt32(fields[1]);
            for (; x < index + amici; x++)
            {
                int indexOf;

                if((indexOf = usernameFound.IndexOf(fields[x])) >= 0)
                    message = message.Insert(message.IndexOf(fields[x]) + fields[x].Length + 1, "online/" + usernameFound[indexOf + 1] + "/");
                else
                    message = message.Insert(message.IndexOf(fields[x]) + fields[x].Length + 1, "offline/-/");
            }

            index += amici + 1;
            amici = Convert.ToInt32(fields[2 + amici]);
            x++;
            for (; x < index + amici; x++)
            {
                int indexOf;

                if ((indexOf = usernameFound.IndexOf(fields[x])) >= 0)
                    message = message.Insert(message.IndexOf(fields[x]) + fields[x].Length + 1, "online/" + usernameFound[indexOf + 1] + "/");
                else
                    message = message.Insert(message.IndexOf(fields[x]) + fields[x].Length + 1, "offline/-/");
            }

            return message;
        }
    }
}

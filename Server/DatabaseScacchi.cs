using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;

namespace ChessServer
{
    public class DatabaseScacchi
    {
        public string _connectionString;

        public DatabaseScacchi(string connectionString)
        {
            _connectionString = connectionString;

            // Controlla se il database esiste
            if (File.Exists("db.sdf")) return;

            // Se non esiste crea il database
            SqlCeEngine en = new SqlCeEngine(connectionString);
            en.CreateDatabase();

            SqlCeConnection conn = new SqlCeConnection(connectionString);
            conn.Open();

            SqlCeCommand cmd = conn.CreateCommand();

            string[] commands = {
                @"CREATE TABLE Utente(" +
                        @"codUtente NVARCHAR(32) PRIMARY KEY, " +
                        @"email NVARCHAR(30) NOT NULL, " +
                        @"username NVARCHAR(20) NOT NULL, " +
                        @"password NVARCHAR(32) NOT NULL, " +
                        @"confirmed BIT DEFAULT NULL);",
                @"CREATE TABLE Profilo(" +
                        @"codProfilo NVARCHAR(32) PRIMARY KEY," +
                        @"punteggio INTEGER DEFAULT 1000," +
                        @"partiteV INTEGER DEFAULT 0," +
                        @"partiteN INTEGER DEFAULT 0," +
                        @"partiteP INTEGER DEFAULT 0," +
                        @"avatar NVARCHAR(60) DEFAULT 'pack://application:,,,/img/avatar/256_0.png'," +
                        @"FOREIGN KEY (codProfilo) REFERENCES Utente(codUtente));",
                 @"CREATE TABLE Amicizia(" +
                        @"utenteR NVARCHAR(32) NOT NULL," +
                        @"utenteD NVARCHAR(32) NOT NULL," +
                        @"stato BIT DEFAULT NULL," +
                        @"PRIMARY KEY(utenteR,utenteD)," +
                        @"FOREIGN KEY(utenteR) REFERENCES Utente(codUtente)," +
                        @"FOREIGN KEY(utenteD) REFERENCES Utente(codUtente));",
                  @"CREATE TABLE Partita(" +
                        @"bianco NVARCHAR(32) NOT NULL," +
                        @"nero NVARCHAR(32) NOT NULL," +
                        @"punteggioBianco INTEGER NOT NULL," +
                        @"punteggioNero INTEGER NOT NULL," +
                        @"mosse NVARCHAR(400)," +
                        @"data DATETIME," +
                        @"PRIMARY KEY(bianco,nero,data)," +
                        @"FOREIGN KEY(bianco) REFERENCES Utente(codUtente)," +
                        @"FOREIGN KEY(nero) REFERENCES Utente(codUtente));"
            };

            try
            {
                foreach(string s in commands)
                {
                    cmd.CommandText = s;
                    cmd.Prepare();
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("log.txt", ex.Message.ToString() + Environment.NewLine);
            }
            finally
            {
                conn.Close();
            }
        }

        public string TryLogIn(string username, string password, out string message)
        {
			string code = "LGY/";
			message = "Log in effettuato";
            string hashPassword = CalculateMD5Hash(password);

            SqlCeConnection conn = new SqlCeConnection(_connectionString);
            conn.Open();
            SqlCeCommand cmd = conn.CreateCommand();

            cmd.Parameters.AddWithValue("@USERNAME", username);
            cmd.Parameters.AddWithValue("@PASSWORD", hashPassword);

            try
            {
                cmd.CommandText = @"SELECT COUNT(*) FROM Utente WHERE username = @USERNAME AND password = @PASSWORD";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    if (Convert.ToInt32(reader[0]) == 0)
                    {
                        message = "Credenziali sbagliate";
                        code = "LGN/";
                        return code;
                    }
                }


                cmd.CommandText = @"SELECT COUNT(*) FROM Utente WHERE username = @USERNAME and confirmed='True'";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    if (Convert.ToInt32(reader[0]) == 0)
                    {
                        message = "Per accedere devi prima confermare la registrazione";
                        code = "LGN/";
                        return code;
                    }
                }
            }
            catch (Exception ex)
            {
                File.AppendAllText("log.txt", ex.Message.ToString() + Environment.NewLine);
                code = "LGN/";
                message = "Errore nell'accesso";
            }
            finally
            {
                conn.Close();
            }

            return code;
        }

        public string TryRegister(string emailTo, string username, string password, out string message)
        {            
            const string ServerSmtp = "smtp.gmail.com";
            const int PortaSmtp = 587;
            const string EmailFrom = "scacchi.fauser@gmail.com";
            const string pw = "scacchi2018";
            string hashEmail = CalculateMD5Hash(emailTo);
            string hashPassword = CalculateMD5Hash(password);
            string code = "RGY/";
            message = "Registrazione effettuata";
			emailTo = emailTo.ToLower();

            SqlCeConnection conn = new SqlCeConnection(_connectionString);
            conn.Open();

            SqlCeCommand cmd = conn.CreateCommand();

            cmd.Parameters.AddWithValue("@CODUTENTE", hashEmail);
            cmd.Parameters.AddWithValue("@EMAIL", emailTo);
            cmd.Parameters.AddWithValue("@USERNAME", username);
            cmd.Parameters.AddWithValue("@PASSWORD", hashPassword);

            try
            {
                cmd.CommandText = "SELECT COUNT(*) FROM Utente WHERE email = @EMAIL";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    if (Convert.ToInt32(reader[0]) != 0)
                    {
                        message = "Questa email e' gia stata utilizzata";
                        code = "RGN/";
                        return code;
                    }
                }

                cmd.CommandText = @"SELECT COUNT(*) FROM Utente WHERE username = @USERNAME";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    if (Convert.ToInt32(reader[0]) != 0)
                    {
                        message = "Questo username e' gia stato utilizzato";
                        code = "RGN/";
                        return code;
                    }
                }
         
                cmd.CommandText = "INSERT INTO Utente (codUtente,email,username,password) values (@CODUTENTE,@EMAIL,@USERNAME,@PASSWORD)";
                cmd.Prepare();
                cmd.ExecuteNonQuery();

                cmd.CommandText = "INSERT INTO Profilo (codProfilo) VALUES(@CODUTENTE)";
                cmd.Prepare();
                cmd.ExecuteNonQuery();

                // Creazione del messaggio mail da inviare
                MailMessage mail = new MailMessage();
                mail.From = new MailAddress(EmailFrom);
                mail.To.Add(emailTo);
                mail.Subject = String.Format("Conferma Registrazione", DateTime.Now);
                mail.Body = "Click on the link: <a href='http://localhost/registrazione.html?codUtente=" + hashEmail + "'>Conferma registrazione</a>";
                mail.IsBodyHtml = true;

                // Invio del messaggio usando la classe SmtpClient
                SmtpClient smtp = new SmtpClient(ServerSmtp, PortaSmtp);
                smtp.Credentials = new NetworkCredential(EmailFrom, pw);
                smtp.EnableSsl = true;
                smtp.Send(mail);

            }
            catch (Exception ex)
            {
                File.AppendAllText("log.txt", ex.Message.ToString() + Environment.NewLine);
                code = "RGN/";
                message = "Errore nella registrazione";
            }
            finally
            {
                conn.Close();
            }

            return code;
        }

        public void ConfirmRegister(string codUtente)
        {
            // Viene aperta la connessione al database
            SqlCeConnection conn = new SqlCeConnection(_connectionString);
            conn.Open();
            SqlCeCommand cmd = conn.CreateCommand();

            cmd.Parameters.AddWithValue("@CODUTENTE", codUtente);

            // Viene creato il comando query da inviare al database,
            // in questo caso viene modificato il campo confirmed quando il server riceve la conferma inoltrata per e-mail
            cmd.CommandText = @"UPDATE Utente SET confirmed='true' WHERE codUtente = @CODUTENTE;";

            try
            {
                cmd.Prepare();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                File.AppendAllText("log.txt", ex.Message.ToString() + Environment.NewLine);
            }
            finally
            {
                conn.Close();
            }
        }

        public string TryChangePassword(string username, string newPassword, out string message)
        {
            string code = "CPY/";
            message = "Cambio password avvenuto con successo";

            SqlCeConnection conn = new SqlCeConnection(_connectionString);
            conn.Open();
            SqlCeCommand cmd = conn.CreateCommand();

            cmd.Parameters.AddWithValue("@USERNAME", username);
            cmd.Parameters.AddWithValue("@PASSWORD", newPassword);

            try
            {
                cmd.CommandText = @"UPDATE Utente SET password = @PASSWORD WHERE username = @USERNAME";
                cmd.Prepare();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                File.AppendAllText("log.txt", ex.Message.ToString() + Environment.NewLine);
                code = "CPN/";
                message = "Errore durante la procedura di cambio password";
            }
            finally
            {
                conn.Close();
            }

            return code;
        }

        public string GetUserSettings(string username, out string message)
        {
            string code = "PRO/";
            message = "pack://application:,,,/img/avatar/256_0.png";

            SqlCeConnection conn = new SqlCeConnection(_connectionString);
            conn.Open();
            SqlCeCommand cmd = conn.CreateCommand();

            cmd.Parameters.AddWithValue("@USERNAME", username);

            try
            {  
                cmd.CommandText = "SELECT avatar,punteggio,partiteV,partiteN,partiteP FROM Profilo, Utente WHERE username = @USERNAME AND codUtente = codProfilo";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    message = String.Empty;
                    for (int i = 0; i < reader.FieldCount; i++)
                        message +=  reader[i] + "/"; 
                }
            }
            catch (Exception ex)
            {
                message += "/errore";
                File.AppendAllText("log.txt", ex.Message.ToString() + Environment.NewLine);
            }
            finally
            {
                conn.Close();
            }

            return code;
        }

        public string GetOpponentSettings(string username, out string message) {
            string code = "OPP/";
            message = "pack://application:,,,/img/avatar/256_0.png";

            SqlCeConnection conn = new SqlCeConnection(_connectionString);
            conn.Open();
            SqlCeCommand cmd = conn.CreateCommand();

            cmd.Parameters.AddWithValue("@USERNAME", username);

            try
            {
                cmd.CommandText = "SELECT avatar,punteggio FROM Profilo, Utente WHERE username = @USERNAME AND codUtente = codProfilo";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    message = String.Empty;
                    for (int i = 0; i < reader.FieldCount; i++)
                        message += reader[i] + "/";
                }
            }
            catch (Exception ex)
            {
                message += "/errore";
                File.AppendAllText("log.txt", ex.Message.ToString() + Environment.NewLine);
            }
            finally
            {
                conn.Close();
            }

            return code;
        }

        public string GetFriendsInformation(string username, out string message) {
            string code = "AMC/";
            message = String.Empty;
            bool temp = true;
            List<string> users = new List<string>();

            SqlCeConnection conn = new SqlCeConnection(_connectionString);
            conn.Open();
            SqlCeCommand cmd = conn.CreateCommand();

            cmd.Parameters.AddWithValue("@USERNAME", username);

            try
            {
                cmd.CommandText = "SELECT COUNT(*) FROM Amicizia,Utente WHERE username = @USERNAME AND codUtente = utenteD AND stato IS NULL";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    message +=  reader[0] + "/";
                }

                cmd.CommandText = "SELECT COUNT(*) FROM Amicizia,Utente WHERE username = @USERNAME AND codUtente = utenteD AND stato = 'true'";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    message += reader[0] + "/";
                    if (Convert.ToInt32(reader[0]) == 0) temp = false;
                }


                if (temp) {
                    cmd.CommandText = "SELECT utenteR FROM Amicizia,Utente WHERE username = @USERNAME AND codUtente = utenteD AND stato = 'true'";
                    cmd.Prepare();
                    using (SqlCeDataReader reader = cmd.ExecuteReader())
                    {
                        while(reader.Read())
                            users.Add(reader[0].ToString());
                    }
                    foreach (string s in users)
                    {
                        cmd.Parameters.AddWithValue("@S", s);

                        cmd.CommandText = "SELECT username FROM Utente WHERE codUtente = @S";
                        cmd.Prepare();
                        using (SqlCeDataReader reader = cmd.ExecuteReader())
                        {
                            reader.Read();
                            message += reader[0].ToString() + "/";
                        }

                        cmd.Parameters.RemoveAt(1);
                    }                        
                }

                users.Clear();
                temp = true;

                cmd.CommandText = "SELECT COUNT(*) FROM Amicizia,Utente WHERE username = @USERNAME AND codUtente = utenteR AND stato = 'true'";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    message += reader[0] + "/";
                    if (Convert.ToInt32(reader[0]) == 0) temp = false;
                }

                if (temp) {
                    cmd.CommandText = "SELECT utenteD FROM Amicizia,Utente WHERE username = @USERNAME AND codUtente = utenteR AND stato = 'true'";
                    cmd.Prepare();
                    using (SqlCeDataReader reader = cmd.ExecuteReader())
                    {
                        while(reader.Read())
                            users.Add(reader[0].ToString());
                    }

                    foreach (string s in users)
                    {
                        cmd.Parameters.AddWithValue("@S", s);

                        cmd.CommandText = "SELECT username FROM Utente WHERE codUtente = @S";
                        cmd.Prepare();
                        using (SqlCeDataReader reader = cmd.ExecuteReader())
                        {
                            reader.Read();
                            message += reader[0].ToString() + "/";
                        }

                        cmd.Parameters.RemoveAt(1);
                    }
                }
            }
            catch (Exception ex)
            {
                message += "/errore";
                File.AppendAllText("log.txt", ex.Message.ToString() + Environment.NewLine);
            }
            finally
            {
                conn.Close();
            }

            return code;
        }

        public string TryChangeAvatar(string username, string newAvatar, out string message) {
            string code = "CAY/";
            message = "avatar cambiato con successo";
            string codProfilo = String.Empty;

            SqlCeConnection conn = new SqlCeConnection(_connectionString);
            conn.Open();
            SqlCeCommand cmd = conn.CreateCommand();

            cmd.Parameters.AddWithValue("@USERNAME", username);
            cmd.Parameters.AddWithValue("@AVATAR", newAvatar);

            try
            {
                cmd.CommandText = "SELECT codUtente FROM Utente WHERE username = @USERNAME";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    codProfilo = reader[0].ToString();
                }

                cmd.Parameters.AddWithValue("@CODPROFILO", codProfilo);

                cmd.CommandText = @"UPDATE Profilo SET avatar = @AVATAR WHERE codProfilo = @CODPROFILO";
                cmd.Prepare();
                cmd.ExecuteNonQuery();

                message += "/" + newAvatar; 
            }
            catch (Exception ex)
            {
                File.AppendAllText("log.txt", ex.Message.ToString() + Environment.NewLine);
                code = "CAN/";
                message = "Errore durante la procedura di cambio avatar";
            }
            finally
            {
                conn.Close();
            }

            return code;
        }

        public string SearchFriends(string research, string username, out string message)
        {
            string code = "SRC";
            message = String.Empty;

            SqlCeConnection conn = new SqlCeConnection(_connectionString);
            conn.Open();
            SqlCeCommand cmd = conn.CreateCommand();

            cmd.Parameters.AddWithValue("@RESEARCH", research + "%");
            cmd.Parameters.AddWithValue("@USERNAME", username);

            try
            {
                bool temp = true;

                cmd.CommandText = "SELECT COUNT(username) FROM Utente WHERE username LIKE @RESEARCH AND username != @USERNAME";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    message += "/" + reader[0] + "/";
                    if (Convert.ToInt32(reader[0]) == 0) temp = false;
                }

                if (temp)
                {
                    cmd.CommandText = "SELECT username FROM Utente WHERE username LIKE @RESEARCH AND username != @USERNAME";
                    cmd.Prepare();
                    using (SqlCeDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                                message += reader[0] + "/";
                    }
                }
            }
            catch (Exception ex)
            {
                message += "/errore";
                File.AppendAllText("log.txt", ex.Message.ToString() + Environment.NewLine);
            }
            finally
            {
                conn.Close();
            }

            return code;
        }

        public string SearchFriendsRequest(string username, out string message)
        {
            string code = "AMR";
            message = String.Empty;

            List<string> codUtente = new List<string>();

            SqlCeConnection conn = new SqlCeConnection(_connectionString);
            conn.Open();
            SqlCeCommand cmd = conn.CreateCommand();

            cmd.Parameters.AddWithValue("@USERNAME", username);

            try
            {
                cmd.CommandText = "SELECT utenteR FROM Amicizia,Utente WHERE username = @USERNAME AND codUtente = utenteD AND stato IS NULL";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read()) codUtente.Add(reader[0].ToString());
                }

                message = "/" + codUtente.Count.ToString();

                if (codUtente.Count == 0) { message = "/0"; conn.Close(); return code; }

                foreach(string s in codUtente)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@CODUTENTE", s);
                    cmd.CommandText = "SELECT username FROM Utente WHERE codUtente = @CODUTENTE";
                    cmd.Prepare();
                    using (SqlCeDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read()) message += "/" + reader[0];
                    }
                }
            }
            catch (Exception ex)
            {
                message += "/errore";
                File.AppendAllText("log.txt", ex.Message.ToString() + Environment.NewLine);
            }
            finally
            {
                conn.Close();
            }

            return code;
        }

        public string GetClassifiche(string username, out string message)
        {
            string code = "CLA";
            message = String.Empty;

            SqlCeConnection conn = new SqlCeConnection(_connectionString);
            conn.Open();
            SqlCeCommand cmd = conn.CreateCommand();

            try
            {
                cmd.CommandText = "SELECT COUNT(*) FROM Utente";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    if (Convert.ToInt32(reader[0]) >= 10) message += "/20";
                    else message += "/" + Convert.ToInt32(reader[0]) * 2;
                }

                cmd.CommandText = "SELECT username, punteggio FROM Utente U INNER JOIN Profilo P ON U.codUtente = P.codProfilo ORDER BY punteggio DESC";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    int count = 1;
                    while (reader.Read())
                    {
                        message += "/" + reader[0] + "/" + reader[1];

                        if (count >= 10) break;
                        count++;
                    }
                }
            }
            catch (Exception ex)
            {
                message += "/errore";
                File.AppendAllText("log.txt", ex.Message.ToString() + Environment.NewLine);
            }
            finally
            {
                conn.Close();
            }

            return code;
        }

        public string FriendRequest(string richiedente, string destinatario, out string message) {

            string code = "SFR/";
            message = String.Empty;     

            SqlCeConnection conn = new SqlCeConnection(_connectionString);
            conn.Open();
            SqlCeCommand cmd = conn.CreateCommand();

            cmd.Parameters.AddWithValue("@R", richiedente);
            cmd.Parameters.AddWithValue("@D", destinatario);

            try
            {
                cmd.CommandText = "SELECT codUtente FROM Utente WHERE username = @R OR username = @D";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    richiedente = reader[0].ToString();
                    reader.Read();
                    destinatario = reader[0].ToString();
                }

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@R", richiedente);
                cmd.Parameters.AddWithValue("@D", destinatario);

                cmd.CommandText = "SELECT stato FROM Amicizia WHERE (utenteR = @R AND utenteD = @D) OR (utenteR = @D AND utenteD = @R)";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        message = reader[0].ToString();
                    }
                    else
                    {
                        cmd.CommandText = "INSERT INTO Amicizia(utenteR, utenteD) VALUES (@R,@D)";
                        cmd.Prepare();
                        cmd.ExecuteNonQuery();
                        message = "ok";
                    }
                }

            }
            catch (Exception ex)
            {
                message += "/errore";
                File.AppendAllText("log.txt", ex.Message.ToString() + Environment.NewLine);
            }
            finally
            {
                conn.Close();
            }

            return code;
        }

        public string AcceptFriendRequest(string richiedente, string destinatario, out string message) {
            string code = "AFR/";
            message = String.Empty;

            SqlCeConnection conn = new SqlCeConnection(_connectionString);
            conn.Open();
            SqlCeCommand cmd = conn.CreateCommand();

            cmd.Parameters.AddWithValue("@R", richiedente);
            cmd.Parameters.AddWithValue("@D", destinatario);

            try
            {
                cmd.CommandText = "SELECT codUtente FROM Utente WHERE username = @R OR username = @D";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    richiedente = reader[0].ToString();
                    reader.Read();
                    destinatario = reader[0].ToString();
                }

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@R", richiedente);
                cmd.Parameters.AddWithValue("@D", destinatario);

                cmd.CommandText = "UPDATE Amicizia SET stato = 'true' WHERE utenteR = @R AND utenteD = @D";
                cmd.Prepare();
                cmd.ExecuteNonQuery();

                message = "ok";

            }
            catch (Exception ex)
            {
                message += "/errore";
                File.AppendAllText("log.txt", ex.Message.ToString() + Environment.NewLine);
            }
            finally
            {
                conn.Close();
            }

            return code;
        }

        public string DenyFriendRequest(string richiedente, string destinatario, out string message)
        {
            string code = "DFR/";
            message = String.Empty;

            SqlCeConnection conn = new SqlCeConnection(_connectionString);
            conn.Open();
            SqlCeCommand cmd = conn.CreateCommand();

            cmd.Parameters.AddWithValue("@R", richiedente);
            cmd.Parameters.AddWithValue("@D", destinatario);

            try
            {
                cmd.CommandText = "SELECT codUtente FROM Utente WHERE username = @R OR username = @D";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    richiedente = reader[0].ToString();
                    reader.Read();
                    destinatario = reader[0].ToString();
                }

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@R", richiedente);
                cmd.Parameters.AddWithValue("@D", destinatario);

                cmd.CommandText = "UPDATE Amicizia SET stato = 'false' WHERE utenteR = @R AND utenteD = @D";
                cmd.Prepare();
                cmd.ExecuteNonQuery();

                message = "ok";

            }
            catch (Exception ex)
            {
                message += "/errore";
                File.AppendAllText("log.txt", ex.Message.ToString() + Environment.NewLine);
            }
            finally
            {
                conn.Close();
            }

            return code;
        }

        public void PutMatchInReplay(string record) {

            int p1;
            int p2;

            string u1 = record.Substring(0,record.IndexOf('/'));
            record = record.Substring(record.IndexOf('/') + 1, record.Length - (record.IndexOf('/') + 1));
            string u2 = record.Substring(0, record.IndexOf('/'));
            record = record.Substring(record.IndexOf('/') + 1, record.Length - (record.IndexOf("/") + 1));
            string dateTime = record.Substring(0, record.IndexOf("--"));
            record = record.Substring(record.IndexOf("--") + 2, record.Length - (record.IndexOf("--") + 2));

            SqlCeConnection conn = new SqlCeConnection(_connectionString);
            conn.Open();
            SqlCeCommand cmd = conn.CreateCommand();

            cmd.Parameters.AddWithValue("@B", u1);
            cmd.Parameters.AddWithValue("@N", u2);

            try
            {
                cmd.CommandText = "SELECT codUtente FROM Utente WHERE username = @B";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    u1 = reader[0].ToString();
                }

                cmd.CommandText = "SELECT codUtente FROM Utente WHERE username = @N";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    u2 = reader[0].ToString();
                }

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@B", u1);
                cmd.Parameters.AddWithValue("@N", u2);

                cmd.CommandText = "SELECT punteggio FROM Profilo WHERE codProfilo = @B";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    p1 = Convert.ToInt32(reader[0]);
                }

                cmd.CommandText = "SELECT punteggio FROM Profilo WHERE codProfilo = @N";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    p2 = Convert.ToInt32(reader[0]);
                }

                cmd.Parameters.AddWithValue("@PB", p1);
                cmd.Parameters.AddWithValue("@PN", p2);
                cmd.Parameters.AddWithValue("@MOSSE", record);
                cmd.Parameters.AddWithValue("@DATA", dateTime);

                cmd.CommandText = "INSERT INTO Partita (bianco,nero,punteggioBianco,punteggioNero,mosse,data) VALUES (@B,@N,@PB,@PN,@MOSSE,@DATA)";
                cmd.Prepare();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            { 
                File.AppendAllText("log.txt", ex.Message.ToString() + Environment.NewLine);
            }
            finally
            {
                conn.Close();
            }
        }

        public void UpdateScore(string winner, string loser, bool pareggio)
        {
            int pw;
            int pl;

            SqlCeConnection conn = new SqlCeConnection(_connectionString);
            conn.Open();
            SqlCeCommand cmd = conn.CreateCommand();

            cmd.Parameters.AddWithValue("@W", winner);
            cmd.Parameters.AddWithValue("@L", loser);

            try
            {
                cmd.CommandText = "SELECT codUtente FROM Utente WHERE username = @W";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    winner = reader[0].ToString();
                }

                cmd.CommandText = "SELECT codUtente FROM Utente WHERE username = @L";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    loser = reader[0].ToString();
                }

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@W", winner);
                cmd.Parameters.AddWithValue("@L", loser);

                cmd.CommandText = "SELECT punteggio FROM Profilo WHERE codProfilo = @W";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    pw = Convert.ToInt32(reader[0]);
                }

                cmd.CommandText = "SELECT punteggio FROM Profilo WHERE codProfilo = @L";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    pl = Convert.ToInt32(reader[0]);
                }

                int delta = (int)(0.20 * Math.Abs(pw - pl));

                if (!pareggio)
                    delta += 20;

                if (pw < pl)
                {
                    cmd.Parameters.AddWithValue("@PW", pw + delta);
                    cmd.Parameters.AddWithValue("@PL", pl - delta);
                }
                else
                {
                    cmd.Parameters.AddWithValue("@PW", pw - delta);
                    cmd.Parameters.AddWithValue("@PL", pl + delta);
                }

                cmd.CommandText = "UPDATE Profilo SET punteggio = @PW WHERE codProfilo = @W";
                cmd.Prepare();
                cmd.ExecuteNonQuery();

                cmd.CommandText = "UPDATE Profilo SET punteggio = @PL WHERE codProfilo = @L";
                cmd.Prepare();
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                File.AppendAllText("log.txt", ex.Message.ToString() + Environment.NewLine);
            }
            finally
            {
                conn.Close();
            }
        }

        public string GetReplay(string date, string bianco, string nero, out string message)
        {
            string code = "SHR";
            message = String.Empty;

            SqlCeConnection conn = new SqlCeConnection(_connectionString);
            conn.Open();
            SqlCeCommand cmd = conn.CreateCommand();

            date = date.Replace('-', '/');

            try
            {
                cmd.Parameters.AddWithValue("@BIANCO", bianco);
                cmd.Parameters.AddWithValue("@NERO", nero);

                cmd.CommandText = "SELECT codUtente FROM Utente WHERE username = @BIANCO";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    bianco = reader[0].ToString();
                }

                cmd.CommandText = "SELECT codUtente FROM Utente WHERE username = @NERO";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    nero = reader[0].ToString();      
                }

                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@NERO", nero);
                cmd.Parameters.AddWithValue("@BIANCO", bianco);
                cmd.Parameters.AddWithValue("@DATE", date);

                cmd.CommandText = "SELECT mosse FROM Partita WHERE data = @DATE AND bianco = @BIANCO AND nero = @NERO";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    message += "/" + reader[0];
                }

            }
            catch (Exception ex)
            {
                File.AppendAllText("log.txt", ex.Message.ToString() + Environment.NewLine);
            }
            finally
            {
                conn.Close();
            }

            return code;
        }

        public string GetReplayList(string filter, out string message)
        {
            string code = "REP";
            message = String.Empty;

            Dictionary<string, string> codToUser = new Dictionary<string, string>();
            List<string> users = new List<string>();
            
            SqlCeConnection conn = new SqlCeConnection(_connectionString);
            conn.Open();
            SqlCeCommand cmd = conn.CreateCommand();

            try
            {
                cmd.CommandText = "SELECT bianco, nero FROM Partita ORDER BY data DESC";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if(users.IndexOf(reader[0].ToString()) < 0)
                            users.Add(reader[0].ToString());
                        if (users.IndexOf(reader[1].ToString()) < 0)
                            users.Add(reader[1].ToString());
                    }
                }

                foreach(string s in users)
                {
                    cmd.Parameters.AddWithValue("@CODUTENTE", s);
                    cmd.CommandText = "SELECT username FROM Utente WHERE codUtente = @CODUTENTE";
                    cmd.Prepare();
                    using (SqlCeDataReader reader = cmd.ExecuteReader())
                    {
                        reader.Read();
                        codToUser.Add(s, reader[0].ToString());
                    }
                    cmd.Parameters.Clear();
                }

                cmd.CommandText = "SELECT data, bianco, punteggioBianco, nero, punteggioNero FROM Partita ORDER BY data DESC";
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string data = reader[0].ToString().Replace('/', '-');
                        message += "/" + data + "/" + codToUser[reader[1].ToString()] + "/" + reader[2].ToString() + "/" +
                             codToUser[reader[3].ToString()] + "/" + reader[4].ToString();
                    }
                }

            }
            catch (Exception ex)
            {
                File.AppendAllText("log.txt", ex.Message.ToString() + Environment.NewLine);
            }
            finally
            {
                conn.Close();
            }

            return code;
        }

        private static string CalculateMD5Hash(string testo)
        {
            MD5 md5 = MD5.Create();
            byte[] testoBytes = Encoding.UTF8.GetBytes(testo);
            byte[] hash = md5.ComputeHash(testoBytes);

            StringBuilder sb = new StringBuilder();

            // Viene costruita una stringa contenente il risultato esadecimale della funzione di hash MD5
            for (int i = 0; i < hash.Length; i++)
                sb.Append(hash[i].ToString("X2"));

            return sb.ToString();
        }

    }
}

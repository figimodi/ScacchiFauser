using System;
using System.Data.SqlServerCe;

namespace DatabaseManager
{
    class Menu
    {
        const string _connectionString = "DataSource=\"..\\..\\..\\Server\\bin\\Debug\\db.sdf\"; Password=\"Figimodi2000\"";

        static void Main(string[] args)
        {
            string[] commands = {
                @"show table\[nome tabella]",
                @"confirm reg\[username]",
                @"remove reg\[username]",
                @"change password\[username]\[new password]",
                @"change username\[username]\[new username]",
                @"add friend\[username1]\[username2]\[flag]",
                @"mod friend\[username1]\[username2]\[flag]",
                @"dml cmd\[command]",
                @"mod score\[codice profilo/utente]\[new score]"
            };
            string cmd = String.Empty;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("\t\t\tDATABASE MANAGER\n__________________________________________________________________\n\n\n--> ");

            while (cmd.ToLower() != "exit")
            {
                try
                {
                    cmd = Console.ReadLine();
                    Console.WriteLine();
                    string[] fields = cmd.Split('\\');

                    switch (fields[0])
                    {
                        case "help":
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            foreach (string s in commands) Console.WriteLine(s);
                            Console.ForegroundColor = ConsoleColor.White;
                            break;
                        case "sh t":
                        case "show table":
                            ShowTable(fields[1]);
                            break;
                        case "conf r":
                        case "confirm reg":
                            ModFlagRegister(fields[1], true);
                            break;
                        case "rem r":
                        case "remove reg":
                            ModFlagRegister(fields[1], false);
                            break;
                        case "ch p":
                        case "change password":
                            ModPassword(fields[1], fields[2]);
                            break;
                        case "ch u":
                        case "change username":
                            ModUsername(fields[1], fields[2]);
                            break;
                        case "add f":
                        case "add friend":
                            AddFriend(fields[1], fields[2], fields[3]);
                            break;
                        case "mod f s":
                        case "mod friend status":
                            ModFriendStatus(fields[1], fields[2],  fields[3]);
                            break;
                        case "dml cmd":
                            Run(@fields[1]);
                            break;
                        case "mod score":
                        case "mod s":
                            ModPunteggio(fields[1], Convert.ToInt32(fields[2]));
                            break;
                        default:
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("sintassi comando sbagliata");
                            Console.ForegroundColor = ConsoleColor.White;
                            break;
                    }

                    Console.Write("\n--> ");
                }
                catch {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("sintassi comando sbagliata");
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.Write("\n\n-->");
                }
            }
        }

        private static void ShowTable(string table)
        {
            // Viene aperta la connessione al database
            SqlCeConnection conn = new SqlCeConnection(_connectionString);
            conn.Open();
            SqlCeCommand cmd = conn.CreateCommand();

            // Viene creato il comando query da inviare al database,
            // in questo caso viene estratto completamente il contenuto della tabella passata come parametro
            cmd.CommandText = @"SELECT * FROM "+ table;

            try
            {
                cmd.Prepare();
                using (SqlCeDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // Viene creati un array lungo tanto quanto il numero di campi della tabella
                        Object[] values = new Object[reader.FieldCount];
                        reader.GetValues(values);

                        // Ogni oggetto dell'array viene stampato a video
                        foreach (var v in values)
                            Console.Write(v + " ");
                        Console.WriteLine();
                    }
                }
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message.ToString());
                Console.ForegroundColor = ConsoleColor.White;
            }
            finally
            {
                conn.Close();
            }
        }

        private static void ModFlagRegister(string username, bool flag)
        {
            // Viene aperta la connessione al database
            SqlCeConnection conn = new SqlCeConnection(_connectionString);
            conn.Open();
            SqlCeCommand cmd = conn.CreateCommand();

            cmd.Parameters.AddWithValue("@USERNAME", username);

            // Viene creato il comando query da inviare al database,
            // in questo caso viene modificato il campo confirmed quando il server riceve la conferma inoltrata per e-mail
            cmd.CommandText = @"UPDATE Utente SET confirmed='" + flag.ToString() + "' WHERE username = @USERNAME;";

            try
            {
                cmd.Prepare();
                cmd.ExecuteNonQuery(); 
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message.ToString());
                Console.ForegroundColor = ConsoleColor.White;
            }
            finally
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("update avvenuto con successo");
                Console.ForegroundColor = ConsoleColor.White;
                conn.Close();
            }
        }

        private static void ModPassword(string username, string newPass)
        {
            // Viene aperta la connessione al database
            SqlCeConnection conn = new SqlCeConnection(_connectionString);
            conn.Open();
            SqlCeCommand cmd = conn.CreateCommand();

            cmd.Parameters.AddWithValue("@USERNAME", username);
            cmd.Parameters.AddWithValue("@PASSWORD", newPass);

            // Viene creato il comando query da inviare al database,
            // in questo caso viene modificato il campo confirmed quando il server riceve la conferma inoltrata per e-mail
            cmd.CommandText = @"UPDATE Utente SET password = @PASSWORD WHERE username = @USERNAME;";

            try
            {
                cmd.Prepare();
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message.ToString());
                Console.ForegroundColor = ConsoleColor.White;
            }
            finally
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("password modificata con successo");
                Console.ForegroundColor = ConsoleColor.White;
                conn.Close();
            }
        }

        private static void ModUsername(string username, string newUser)
        {
            // Viene aperta la connessione al database
            SqlCeConnection conn = new SqlCeConnection(_connectionString);
            conn.Open();
            SqlCeCommand cmd = conn.CreateCommand();

            cmd.Parameters.AddWithValue("@USERNAME", username);
            cmd.Parameters.AddWithValue("@NEWUSERNAME", newUser);

            // Viene creato il comando query da inviare al database,
            // in questo caso viene modificato il campo confirmed quando il server riceve la conferma inoltrata per e-mail
            cmd.CommandText = @"UPDATE Utente SET username = @NEWUSERNAME WHERE username = @USERNAME;";

            try
            {
                cmd.Prepare();
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message.ToString());
                Console.ForegroundColor = ConsoleColor.White;
            }
            finally
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("username modificato con successo");
                Console.ForegroundColor = ConsoleColor.White;
                conn.Close();
            }
        }

        private static void AddFriend(string user1, string user2, string flag)
        {
            SqlCeConnection conn = new SqlCeConnection(_connectionString);
            conn.Open();
            SqlCeCommand cmd = conn.CreateCommand();

            cmd.Parameters.AddWithValue("@UTENTER", user1);
            cmd.Parameters.AddWithValue("@UTENTED", user2);

            if (flag == "true")
                cmd.Parameters.AddWithValue("@FLAG", true);
            if (flag == "false")
                cmd.Parameters.AddWithValue("@FLAG", false);

            try
            {
                if(flag == "null")
                    cmd.CommandText = "INSERT INTO Amicizia (utenteR,utenteD) values (@UTENTER,@UTENTED)";
                else
                    cmd.CommandText = "INSERT INTO Amicizia (utenteR,utenteD,stato) values (@UTENTER,@UTENTED,@FLAG)";
                cmd.Prepare();
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message.ToString());
                Console.ForegroundColor = ConsoleColor.White;
            }
            finally
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("username modificato con successo");
                Console.ForegroundColor = ConsoleColor.White;
                conn.Close();
            }
        }

        private static void ModFriendStatus(string user1, string user2, string flag) {
            SqlCeConnection conn = new SqlCeConnection(_connectionString);
            conn.Open();
            SqlCeCommand cmd = conn.CreateCommand();

            if (flag == "true")
                cmd.Parameters.AddWithValue("@FLAG", true);
            if(flag == "false")
                cmd.Parameters.AddWithValue("@FLAG", false);

            cmd.Parameters.AddWithValue("@UTENTER", user1);
            cmd.Parameters.AddWithValue("@UTENTED", user2);

            if(flag == "null")
                cmd.CommandText = @"UPDATE Amicizia SET stato = NULL WHERE utenteR = @UTENTER AND utenteD = @UTENTED;";
            else
                cmd.CommandText = @"UPDATE Amicizia SET stato = @FLAG WHERE utenteR = @UTENTER AND utenteD = @UTENTED;";

            try
            {
                cmd.Prepare();
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message.ToString());
                Console.ForegroundColor = ConsoleColor.White;
            }
            finally
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("username modificato con successo");
                Console.ForegroundColor = ConsoleColor.White;
                conn.Close();
            }
        }

        private static void ModPunteggio(string codProfilo, int newScore)
        {
            // Viene aperta la connessione al database
            SqlCeConnection conn = new SqlCeConnection(_connectionString);
            conn.Open();
            SqlCeCommand cmd = conn.CreateCommand();

            cmd.Parameters.AddWithValue("@CODPROFILO", codProfilo);
            cmd.Parameters.AddWithValue("@PUNTEGGIO", newScore);

            // Viene creato il comando query da inviare al database,
            // in questo caso viene modificato il campo confirmed quando il server riceve la conferma inoltrata per e-mail
            cmd.CommandText = @"UPDATE Profilo SET punteggio = @PUNTEGGIO WHERE codProfilo = @CODPROFILO;";

            try
            {
                cmd.Prepare();
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message.ToString());
                Console.ForegroundColor = ConsoleColor.White;
            }
            finally
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("punteggio modificato con successo");
                Console.ForegroundColor = ConsoleColor.White;
                conn.Close();
            }
        }

        public static void Run(string text)
        {
            SqlCeConnection conn = new SqlCeConnection(_connectionString);
            conn.Open();
            SqlCeCommand cmd = conn.CreateCommand();

            // Viene creato il comando query da inviare al database,
            // in questo caso viene estratto completamente il contenuto della tabella passata come parametro
            cmd.CommandText = text;

            try
            {
                cmd.Prepare();
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(e.Message.ToString());
                Console.ForegroundColor = ConsoleColor.White;
            }
            finally
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("successo");
                Console.ForegroundColor = ConsoleColor.White;
                conn.Close();
            }
        }
    }
}

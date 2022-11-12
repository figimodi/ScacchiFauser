using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Client
{
    public partial class MainWindow : Window
    {
        SslStream _networkSslStream;
        BackgroundWorker _loading = new BackgroundWorker();
        BackgroundWorker _gestoreEventi = new BackgroundWorker();
        Match _match;
        Match _replay;
        string _username;
        byte[] _buffer = new byte[1024];
        string _stanza;
        string _replayMoves;
        string _replayBackUp;
        bool _turn = false;
        bool _eom = false;
        bool _profiloFunctions = true;
        int _oldX = -1;
        int _oldY = -1;
        int _xPromotion = -1;
        int _counterPromotions = 0;
        int _replayIndex = 0;
        static readonly string _nomeCertificatoServer = "Server";

        public MainWindow()
        { 
            InitializeComponent();
            TryToConnectChessServer();

            // metodi backgroundworker
            _loading.DoWork += loadingStart;
            _loading.ProgressChanged += loadingChanged;
            _loading.WorkerReportsProgress = true;
            _loading.WorkerSupportsCancellation = true;

            _gestoreEventi.DoWork += gestoreEventiStart;
            _gestoreEventi.ProgressChanged += gestoreEventiChanged;
            _gestoreEventi.WorkerReportsProgress = true;
            _gestoreEventi.RunWorkerAsync();
        }

        #region CANVAS 1 LOGIN
        private void InitalizeCanvasLog(object sender, DependencyPropertyChangedEventArgs e)
        {
            lblError.Content = String.Empty;
            txtUser.Text = "Username";
            txtUser.Foreground = Brushes.Gray;
            txtPassword.Visibility = Visibility.Visible;
            pswPassword.Visibility = Visibility.Hidden;
            pswPassword.Password = String.Empty;
            txtPassword.Text = "Password";
            txtPassword.Foreground = Brushes.Gray;
            btnShowPassword.Visibility = Visibility.Hidden;
        }

        private void TextFocus(object sender, RoutedEventArgs e)
        {
            TextBox t = (TextBox)sender;
            string name = t.Name;
            PasswordBox[] p = { pswPassword, pswPasswordConfirm };
            Button[] b = { btnShowPassword, btnShowPasswordConfirm };

            if (t.Foreground == Brushes.Gray)
            {
                t.Text = String.Empty;
                t.Foreground = Brushes.LightYellow;
                if (name.Contains("Password"))
                {
                    int x = 0;
                    if (name.Contains("Confirm")) x = 1;
                    t.Visibility = Visibility.Hidden;
                    t.Text = "x";
                    p[x].Visibility = Visibility.Visible;
                    p[x].Focus();
                    b[x].Visibility = Visibility.Visible;
                }
            }
        }

        private void TextLostFocus(object sender, RoutedEventArgs e)
        {
            TextBox t = (TextBox)sender;
            string name = t.Name;

            if (t.Text.Trim() == String.Empty)
            {
                t.Foreground = Brushes.Gray;
                switch (name)
                {
                    case "txtEmail":
                        t.Text = "E-mail";
                        break;
                    case "txtUser":
                        t.Text = "Username";
                        break;
                    case "txtPassword":
                        t.Text = "Passowrd";
                        break;
                    case "txtPasswordConfirm":
                        t.Text = "Conferma Password";
                        break;
                }
            }
        }

        private void PasswordLostFocus(object sender, RoutedEventArgs e)
        {
            PasswordBox p = (PasswordBox)sender;
            string name = p.Name;
            TextBox[] t = { txtPassword, txtPasswordConfirm };
            Button[] b = { btnShowPassword, btnShowPasswordConfirm };

            if (p.Password.Trim() == String.Empty)
            {
                int x = 0;
                if (name.Contains("Confirm")) x = 1;
                t[x].Visibility = Visibility.Visible;
                p.Visibility = Visibility.Hidden;
                b[x].Visibility = Visibility.Hidden;
                t[x].Foreground = Brushes.Gray;
                if (x == 0) t[x].Text = "Password";
                else t[x].Text = "Conferma Password";
            }
        }

        private void ShowPassword(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            PasswordBox[] p = { pswPassword, pswPasswordConfirm };
            TextBox[] t = { txtPassword, txtPasswordConfirm };
            int x = 0;
            if (b.Name.Contains("Confirm")) x = 1;

            if (t[x].Visibility == Visibility.Visible)
            {
                p[x].Password = t[x].Text.Trim();
                p[x].Visibility = Visibility.Visible;
                t[x].Visibility = Visibility.Hidden;
            }
            else
            {
                t[x].Text = p[x].Password.Trim();
                p[x].Visibility = Visibility.Hidden;
                t[x].Visibility = Visibility.Visible;
            }
        }

        private void Return(object sender, RoutedEventArgs e)
        {
            txtEmail.Visibility = Visibility.Hidden;
            txtPasswordConfirm.Visibility = Visibility.Hidden;
            btnLogIn.Visibility = Visibility.Visible;
            btnReturn.Visibility = Visibility.Hidden;
            txtPasswordConfirm.Visibility = Visibility.Hidden;
            btnShowPasswordConfirm.Visibility = Visibility.Hidden;
            pswPasswordConfirm.Visibility = Visibility.Hidden;
            lblError.Content = String.Empty;
        }

        private void LogIn(object sender, RoutedEventArgs e)
        {
            AvviaLogIn();
        }

        private void Registrati(object sender, RoutedEventArgs e)
        {
            if (txtEmail.Visibility == Visibility.Hidden)
            {
                txtEmail.Visibility = Visibility.Visible;
                txtPasswordConfirm.Visibility = Visibility.Visible;
                btnLogIn.Visibility = Visibility.Hidden;
                btnReturn.Visibility = Visibility.Visible;
                lblError.Content = String.Empty;
                txtPasswordConfirm.Text = "Conferma Password";
                pswPasswordConfirm.Password = String.Empty;
                txtEmail.Text = "E-mail";
                txtEmail.Foreground = Brushes.Gray;
                txtPasswordConfirm.Foreground = Brushes.Gray;
            }
            else
            {
                AvviaRegistrazione();
            }
        }

        private void InvioDaTastiera(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (txtEmail.Visibility == Visibility.Visible)
                    AvviaRegistrazione();
                else
                    AvviaLogIn();
            }
        }

        private void AvviaLogIn()
        {
            try
            {
                if (txtUser.Text.Contains("/")) throw new Exception(String.Format("L'username non può contenere il carattere '/'"));
                if (txtPassword.Visibility == Visibility.Visible)
                {
                    if (txtPassword.Text.Contains("/")) throw new Exception(String.Format("La password non può contenere il carattere '/'"));
                }
                else if (pswPassword.Password.Contains("/")) throw new Exception(String.Format("La password non può contenere il carattere '/'"));

                if (txtUser.Foreground == Brushes.Gray || txtUser.Text.Trim() == String.Empty) throw new Exception(String.Format("L'sername non può essere vuoto"));
                if (txtPassword.Foreground == Brushes.Gray || txtPassword.Text.Trim() == String.Empty) throw new Exception(String.Format("La password non può essere vuota"));
                if (pswPassword.Foreground == Brushes.Gray || pswPassword.Password.Trim() == String.Empty) throw new Exception(String.Format("La password non può essere vuota"));
                byte[] dataTx = Encoding.UTF8.GetBytes("LOG/" + txtUser.Text.Trim() + "/" + pswPassword.Password.Trim());

                try
                {
                    _networkSslStream.Write(dataTx);
                }
                catch
                {
                    TryToConnectChessServer();
                    lblError.Content = "Errore di connessione, server irraggiungibile";
                }
            }
            catch (Exception ex)
            {
                lblError.Content = ex.Message.ToString();
            }
        }

        private void AvviaRegistrazione() {
            try
            {
                if (txtEmail.Text.Contains("/")) throw new Exception(String.Format("L'e-mail non può contenere il carattere '/'"));
                if (txtUser.Text.Contains("/")) throw new Exception(String.Format("L'username non può contenere il carattere '/'"));
                if (txtPassword.Visibility == Visibility.Visible)
                {
                    if (txtPassword.Text.Contains("/")) throw new Exception(String.Format("La password non può contenere il carattere '/'"));
                }
                else if (pswPassword.Password.Contains("/")) throw new Exception(String.Format("La password non può contenere il carattere '/'"));

                if (txtEmail.Foreground == Brushes.Gray || txtEmail.Text.Trim() == String.Empty) throw new Exception(String.Format("L'e-mail non può essere vuota"));
                if (txtUser.Foreground == Brushes.Gray || txtUser.Text.Trim() == String.Empty) throw new Exception(String.Format("L'username non può essere vuoto"));
                if (txtPassword.Foreground == Brushes.Gray || txtPassword.Text.Trim() == String.Empty) throw new Exception(String.Format("La password non può essere vuota"));
                if (txtPasswordConfirm.Foreground == Brushes.Gray || txtPasswordConfirm.Text.Trim() == String.Empty) throw new Exception(String.Format("Le password non coincidono"));
                if (pswPassword.Foreground == Brushes.Gray || pswPassword.Password.Trim() == String.Empty) throw new Exception(String.Format("La password non può essere vuota"));
                if (pswPasswordConfirm.Foreground == Brushes.Gray || pswPasswordConfirm.Password.Trim() == String.Empty) throw new Exception(String.Format("Le password non coincidono"));

                int caso = 0;
                if (txtPassword.Visibility == Visibility.Visible && txtPasswordConfirm.Visibility == Visibility.Hidden) caso = 1;
                if (txtPassword.Visibility == Visibility.Hidden && txtPasswordConfirm.Visibility == Visibility.Visible) caso = 2;
                if (txtPassword.Visibility == Visibility.Hidden && txtPasswordConfirm.Visibility == Visibility.Hidden) caso = 3;
                switch (caso)
                {
                    case 0:
                        if (txtPassword.Text.Trim() != txtPasswordConfirm.Text.Trim()) throw new Exception(String.Format("Le password non coincidono"));
                        break;
                    case 1:
                        if (txtPassword.Text.Trim() != pswPasswordConfirm.Password.Trim()) throw new Exception(String.Format("Le password non coincidono"));
                        break;
                    case 2:
                        if (pswPassword.Password.Trim() != txtPassword.Text.Trim()) throw new Exception(String.Format("Le password non coincidono"));
                        break;
                    case 3:
                        if (pswPassword.Password.Trim() != pswPasswordConfirm.Password.Trim()) throw new Exception(String.Format("Le password non coincidono"));
                        break;
                }

                byte[] dataTx = Encoding.UTF8.GetBytes("REG/" + txtEmail.Text.Trim() + "/" + txtUser.Text.Trim() + "/" + pswPassword.Password.Trim());

                try
                {
                    _networkSslStream.Write(dataTx);
                }
                catch
                {
                    TryToConnectChessServer();
                    lblError.Content = "Errore di connessione, server irraggiungibile";
                }

            }
            catch (Exception ex)
            {
                lblError.Content = ex.Message.ToString();
            }
        }
        #endregion

        #region CANVAS 2 MENU
        private void InitalizeCanvasMenu(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
            {
                btnGiocaOnline.Content = "GIOCA" + Environment.NewLine + "ONLINE";
                btnGiocaConAmici.Content = "GIOCA" + Environment.NewLine + "CON GLI" + Environment.NewLine + "AMICI";
                btnGuardaLive.Content = "GUARDA LE" + Environment.NewLine + "PARTITE LIVE";
                btnGuardaReplay.Content = "RIGUARDA I" + Environment.NewLine + "REPLAY";
                btnClassifiche.Content = "CLASSIFICHE";
                btnProfilo.Content = "PROFILO";
                lblUsername.Content = _username;
                _profiloFunctions = true;

                _networkSslStream.Write(Encoding.UTF8.GetBytes("PRO/" + lblUsername.Content.ToString().Trim()));
            }
        }

        private void btnGiocaOnlineClick(object sender, RoutedEventArgs e)
        {
            Canvas_Menu.Visibility = Visibility.Hidden;
            Canvas_Matchmaking.Visibility = Visibility.Visible;
        }

        private void btnGiocaConAmiciClick(object sender, RoutedEventArgs e)
        {
            Canvas_Menu.Visibility = Visibility.Hidden;
            Canvas_GiocaConAmici.Visibility = Visibility.Visible;
        }

        private void btnGuardaLiveClick(object sender, RoutedEventArgs e)
        {
            
        }

        private void btnGuardaReplayClick(object sender, RoutedEventArgs e)
        {
            Canvas_Replay.Visibility = Visibility.Visible;
            Canvas_Menu.Visibility = Visibility.Hidden;
        }

        private void btnClassificheClick(object sender, RoutedEventArgs e)
        {
            Canvas_Menu.Visibility = Visibility.Hidden;
            Canvas_Classifiche.Visibility = Visibility.Visible;
        }

        private void btnProfiloClick(object sender, RoutedEventArgs e)
        {
            Canvas_Menu.Visibility = Visibility.Hidden;
            Canvas_Profilo.Visibility = Visibility.Visible;
        }

        private void LogOut(object sender, RoutedEventArgs e)
        {
            _networkSslStream.Write(Encoding.UTF8.GetBytes("OUT"));
            Canvas_Menu.Visibility = Visibility.Hidden;
            Canvas_Log.Visibility = Visibility.Visible;
        }
        #endregion

        #region CANVAS 3 MATCHMAKING
        private void InitalizeCanvasMatchmaking(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
            {
                _loading.RunWorkerAsync();
                try
                {
                    byte[] dataTx = Encoding.UTF8.GetBytes("OM1/");
                    _networkSslStream.Write(dataTx);
                }
                catch
                {
                    lblErrorMatchmaking.Content = "Errore di connessione, server irraggiungibile";
                }
            }
        }

        private void Annulla(object sender, RoutedEventArgs e)
        {
            try
            {
                _loading.CancelAsync();

                byte[] dataTx = Encoding.UTF8.GetBytes("CM1/");
                _networkSslStream.Write(dataTx);

                Canvas_Matchmaking.Visibility = Visibility.Hidden;
                Canvas_Menu.Visibility = Visibility.Visible;
            }
            catch
            {
                // Se vi è un errore di connessione provo a riconnettermi
                TryToConnectChessServer();
                Canvas_Matchmaking.Visibility = Visibility.Hidden;
                Canvas_Menu.Visibility = Visibility.Visible;
            }
        }

        private void loadingStart(object sender, DoWorkEventArgs e)
        {
            int i = 0;
            while (!_loading.CancellationPending)
            {
                Thread.Sleep(10);
                _loading.ReportProgress(i++);

                // Rotazione di 180, dopodichè torna parte di nuovo da 1, evitando overflow di i
                if (i == 180) i = 1;
            }
            return;
        }

        private void loadingChanged(object sender, ProgressChangedEventArgs e)
        {
            RotateTransform rotateTransform = new RotateTransform(e.ProgressPercentage);
            imgLoading.RenderTransform = rotateTransform;
        }
        #endregion

        #region CANVAS 4 PARTITA
        private void InitalizeCanvasPartita(object sender, DependencyPropertyChangedEventArgs e)
        {
            imgWin.Visibility = Visibility.Hidden;
            imgDefeat.Visibility = Visibility.Hidden;
            imgDraw.Visibility = Visibility.Hidden;

            lblChat.Document.Blocks.Clear();

            _networkSslStream.Write(Encoding.UTF8.GetBytes("OPP/" + lblInfo1_1.Content.ToString().Trim()));

            InitializeChessboard();
        }
        // inizilizza il canvas della partita, inverte la scacchiera se necessario

        private void InserimentoChatGotFocus(object sender, RoutedEventArgs e)
        {
            TextBox t = (TextBox)sender;

            if (t.Foreground == Brushes.Gray)
            {
                t.Text = String.Empty;
                t.Foreground = Brushes.Black;
            }
        }

        private void InserimentoChatLostFocus(object sender, RoutedEventArgs e)
        {
            TextBox t = (TextBox)sender;

            if (t.Text.Trim() == String.Empty)
            {
                t.Foreground = Brushes.Gray;
                t.Text = "Scrivere un messaggio ...";
            }
        }

        private void SendChatMessage(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                _networkSslStream.Write(Encoding.UTF8.GetBytes("MSG/" + _stanza + "/" + txtTestoInserimento.Text.Trim()));
                PrintChatMessage("Tu", txtTestoInserimento.Text.Trim(), true);
            }
        }

        private void PrintChatMessage(string username, string text, bool player)
        {

            string data = "[" + DateTime.Now.Hour.ToString() + ":";

            if (DateTime.Now.Minute.ToString().Length == 1)
                data += "0" + DateTime.Now.Minute.ToString();
            else
                data += DateTime.Now.Minute.ToString();

            data += "] ";

            TextRange rangeOfDate = new TextRange(lblChat.Document.ContentEnd, lblChat.Document.ContentEnd);
            rangeOfDate.Text = data;
            rangeOfDate.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);
            rangeOfDate.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);

            TextRange rangeOfUsername = new TextRange(lblChat.Document.ContentEnd, lblChat.Document.ContentEnd);
            rangeOfUsername.Text = username + ": ";
            if (player)
                rangeOfUsername.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black);
            else
                rangeOfUsername.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Blue);
            rangeOfUsername.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Bold);

            TextRange rangeOfMessage = new TextRange(lblChat.Document.ContentEnd, lblChat.Document.ContentEnd);
            rangeOfMessage.Text = text + Environment.NewLine;
            rangeOfMessage.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Black);
            rangeOfMessage.ApplyPropertyValue(TextElement.FontWeightProperty, FontWeights.Normal);

            if(username == "Tu")
                txtTestoInserimento.Text = String.Empty;

            lblChat.ScrollToVerticalOffset(lblChat.ExtentHeight);
        }

        private void InitializeChessboard()
        {
            Image[,] pieces = {
                { tNsx, cNsx, aNsx, rgN, reN, aNdx, cNdx, tNdx, pN1, pN2, pN3, pN4, pN5, pN6, pN7, pN8 },
                { tBsx, cBsx, aBsx, rgB, reB, aBdx, cBdx, tBdx, pB1, pB2, pB3, pB4, pB5, pB6, pB7, pB8 }
            };

            Scacchiera.Children.RemoveRange(0, Scacchiera.Children.Count);

            foreach (Image img in pieces)
            {
                Scacchiera.Children.Add(img);
            }

            int z = 0;
            int zp = 1;
            if (!_turn) { z = 7; zp = -1; }


            for (int i = 0; i < 2; i++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x < 8)
                        pieces[i, x].SetValue(Grid.RowProperty, Math.Abs(i * 7 - z));
                    else
                    {
                        if(i == 0)
                            pieces[i, x].SetValue(Grid.RowProperty, Math.Abs(i * 7 - z) + zp);
                        else
                            pieces[i, x].SetValue(Grid.RowProperty, Math.Abs(i * 7 - z) - zp);
                    }
                        
                    pieces[i, x].SetValue(Grid.ColumnProperty, x % 8);
                }
            }

            if (!_turn)
            {
                pieces[0, 4].SetValue(Grid.ColumnProperty, 3);
                pieces[0, 3].SetValue(Grid.ColumnProperty, 4);
                pieces[1, 4].SetValue(Grid.ColumnProperty, 3);
                pieces[1, 3].SetValue(Grid.ColumnProperty, 4);
            }
        }

        private void Chessboard_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_turn)
            {
                var point = Mouse.GetPosition(Scacchiera);

                int newY = 0;
                int newX = 0;
                double accumulatedHeight = 0.0;
                double accumulatedWidth = 0.0;

                // calclo il numero della riga
                foreach (var rowDefinition in Scacchiera.RowDefinitions)
                {
                    accumulatedHeight += rowDefinition.ActualHeight;
                    if (accumulatedHeight >= point.Y)
                        break;
                    newY++;
                }

                // calcolo il numero della colonna
                foreach (var columnDefinition in Scacchiera.ColumnDefinitions)
                {
                    accumulatedWidth += columnDefinition.ActualWidth;
                    if (accumulatedWidth >= point.X)
                        break;
                    newX++;
                }

                if (newX != _oldX || newY != _oldY)
                {
                    if (_match.SelectCase(newX, newY)) {

                        if (_oldX == -1) return;

                        if (_match.AllowMove(_oldX, _oldY, newX, newY ,out int flagMove))
                        {
                            UIElement element;
                            string additionalParams = String.Empty;
                            // flag:
                            // -1 : null, 1: mangiata, 2:3:4:5 : arrocco, 6: en passant, 7 pedone a fine scacchiera
                            if (flagMove == 1)
                            {
                                element = Scacchiera.Children.Cast<UIElement>().FirstOrDefault(ex => Grid.GetColumn(ex) == newX && Grid.GetRow(ex) == newY);
                                Scacchiera.Children.Remove(element);
                            }
                            if (flagMove > 1 && flagMove < 6)
                            {
                                if (flagMove < 4)
                                {
                                    element = Scacchiera.Children.Cast<UIElement>().FirstOrDefault(ex => Grid.GetColumn(ex) == 0 && Grid.GetRow(ex) == 7);
                                    Grid.SetColumn(element, flagMove);
                                    additionalParams = "70" + (7 - flagMove) + "0";
                                }
                                else {
                                    element = Scacchiera.Children.Cast<UIElement>().FirstOrDefault(ex => Grid.GetColumn(ex) == 7 && Grid.GetRow(ex) == 7);
                                    Grid.SetColumn(element, flagMove);
                                    additionalParams = "00" + (7 - flagMove) + "0";
                                }
                            }
                            if (flagMove == 6)
                            {
                                element = Scacchiera.Children.Cast<UIElement>().FirstOrDefault(ex => Grid.GetColumn(ex) == newX && Grid.GetRow(ex) == 3);
                                Scacchiera.Children.Remove(element);
                                additionalParams =  (7 - newX).ToString() +
                                                    (7 - newY).ToString() +
                                                    (7 - newX).ToString() + 
                                                    (7 - newY - 1).ToString() + 
                                                    (7 - newX).ToString() + 
                                                    (7 - newY - 1).ToString() + 
                                                    (7 - newX).ToString() + 
                                                    (7 - newY).ToString();
                            }
                            element = Scacchiera.Children.Cast<UIElement>().FirstOrDefault(ex => Grid.GetColumn(ex) == _oldX && Grid.GetRow(ex) == _oldY);
                            Grid.SetRow(element, newY);
                            Grid.SetColumn(element, newX);

                            if(newY == 0 && element.GetValue(NameProperty).ToString().Substring(0,1) == "p")
                            {
                                _turn = false;
                                Promotion.Visibility = Visibility.Visible;
                                if (element.GetValue(NameProperty).ToString().Substring(1, 2).Contains("N")) InitializeGridPromotion(true);
                                else InitializeGridPromotion(false);
                                _xPromotion = newX;
                                return;
                            }

                            _networkSslStream.Write(Encoding.UTF8.GetBytes("MOV/" + _stanza + "/" + (7 - _oldX) + (7 - _oldY) + (7 - newX) + (7 - newY) + additionalParams));
                            _turn = false;
                        }
                    }
                    else
                    {
                        _oldX = newX;
                        _oldY = newY;
                    }
                }
            }
        }
        // evento click sulla tastiera
        // le procedure: 
        // 1) si verifica la possibilità di eseguire la mossa, in termini di movimento adeguato
        // 2) si aggiorna lo stato dell'array e delle pedine
        // 3) invio dei dati al server

        private void InitializeGridPromotion(bool black) {

            Image[,] pieces = {
                { tNsxP, cNsxP, aNsxP, rgNP },
                { tBsxP, cBsxP, aBsxP, rgBP }
            };
            int i = 0;

            foreach (Image img in pieces)
            {
                if (PromotionPieces.Children.IndexOf(img) < 0)
                    PromotionPieces.Children.Add(img);
            }

            if (black) {
                i = 1;
                lblPromozione.Foreground = Brushes.Black;
                lblPromozione.Background = Brushes.White;
                Promotion.Background = Brushes.White;
            }

            for (int x = 0; x < 4; x++) PromotionPieces.Children.Remove(pieces[i, x]);

            
        }

        private void Promotion_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var point = Mouse.GetPosition(PromotionPieces);
            bool black = false;
            String addParam = String.Empty;
            int x = 0;
            double accumulatedWidth = 0.0;


            // calcolo il numero della colonna
            foreach (var columnDefinition in PromotionPieces.ColumnDefinitions)
            {
                accumulatedWidth += columnDefinition.ActualWidth;
                if (accumulatedWidth >= point.X)
                    break;
                x++;
            }

            UIElement element = Scacchiera.Children.Cast<UIElement>().FirstOrDefault(ex => Grid.GetColumn(ex) == _xPromotion && Grid.GetRow(ex) == 0);
            Scacchiera.Children.Remove(element);
            Image addImage = new Image();

            element = PromotionPieces.Children.Cast<UIElement>().FirstOrDefault(ex => Grid.GetColumn(ex) == x && Grid.GetRow(ex) == 0);
            if (element.GetValue(NameProperty).ToString().Substring(1, 2).Contains("N")) black = true;

            for (int cp = _counterPromotions; cp < 0; cp--)
                element.SetValue(NameProperty, NameProperty + "P");

            _counterPromotions++;

            switch (++x)
            {
                case 1:
                    if (black) addImage.Source = tNsx.Source;
                    else addImage.Source = tBsx.Source;
                    break;
                case 2:
                    if (black) addImage.Source = cNsx.Source;
                    else addImage.Source = cBsx.Source;
                    break;
                case 3:
                    if (black) addImage.Source = aNsx.Source;
                    else addImage.Source = aBsx.Source;
                    break;
                case 4:
                    if (black) addImage.Source = rgN.Source;
                    else addImage.Source = rgB.Source;
                    break;
            }

            addImage.Height = 80;
            addImage.Width = 80;
            Scacchiera.Children.Add(addImage);
            Grid.SetRow(addImage, 0);
            Grid.SetColumn(addImage, _xPromotion);

            _match.MakePromotion(x, _xPromotion, out addParam);

            addParam += addParam;
            addParam += element.GetValue(NameProperty).ToString();
            addParam += (7 - _xPromotion).ToString();

            _networkSslStream.Write(Encoding.UTF8.GetBytes("MOV/" + _stanza + "/" + (7 - _oldX) + (7 - _oldY) + (7 - _xPromotion) + 7 + addParam));

            Promotion.Visibility = Visibility.Hidden;
        }

        private void MoveOpponenet(string move)
        {
            string appo = String.Empty;

            // UTF8 7 = 48, 6 = 49, 5 = 50 ...
            int[] coord = {
                Convert.ToInt32(move[0]) - 55,
                Convert.ToInt32(move[1]) - 55,
                Convert.ToInt32(move[2]) - 55,
                Convert.ToInt32(move[3]) - 55
            };
            for (int i = 0; i < 4; i++)
            {
                if (coord[i] < 0) coord[i] *= -1;
                coord[i] = 7 - coord[i];
            }

            UIElement element = new UIElement();
            Image addImage = new Image();

            // in caso di fondo scacchiera arriva un messaggio speciale composto da un non spostamento
            if (move.Substring(0, 2) == move.Substring(2, 2))
            {
                element = Scacchiera.Children.Cast<UIElement>().FirstOrDefault(ex => Grid.GetColumn(ex) == Convert.ToInt32(move.Substring(move.Length - 1, 1)) && Grid.GetRow(ex) == 7);
                Scacchiera.Children.Remove(element);

                switch (move.Substring(4, 1))
                {
                    case "t":
                        if (move.Substring(5, 2).Contains("N")) addImage.Source = tNsx.Source;
                        else addImage.Source = tBsx.Source;
                        _match.UpgradePiece(coord[0], coord[1], 21);
                        break;
                    case "c":
                        if (move.Substring(5, 2).Contains("N")) addImage.Source = cNsx.Source;
                        else addImage.Source = cBsx.Source;
                        _match.UpgradePiece(coord[0], coord[1], 22);
                        break;
                    case "a":
                        if (move.Substring(5, 2).Contains("N")) addImage.Source = aNsx.Source;
                        else addImage.Source = aBsx.Source;
                        _match.UpgradePiece(coord[0], coord[1], 23);
                        break;
                    case "r":
                        if (move.Substring(5, 2).Contains("N")) addImage.Source = rgN.Source;
                        else addImage.Source = rgB.Source;
                        _match.UpgradePiece(coord[0], coord[1], 24);
                        break;
                }

                addImage.Height = 80;
                addImage.Width = 80;
                Scacchiera.Children.Add(addImage);
                Grid.SetRow(addImage, 7);
                Grid.SetColumn(addImage, Convert.ToInt32(move.Substring(move.Length - 1, 1)));

                _match.MoveOpponent(coord[0], coord[1], Convert.ToInt32(move.Substring(move.Length - 1, 1)), 7);
            }
            else {
                if (_match.MoveOpponent(coord[0], coord[1], coord[2], coord[3]))
                {
                    element = Scacchiera.Children.Cast<UIElement>().FirstOrDefault(ex => Grid.GetColumn(ex) == coord[2] && Grid.GetRow(ex) == coord[3]);
                    Scacchiera.Children.Remove(element);
                }
                element = Scacchiera.Children.Cast<UIElement>().FirstOrDefault(ex => Grid.GetColumn(ex) == coord[0] && Grid.GetRow(ex) == coord[1]);
                Grid.SetRow(element, coord[3]);
                Grid.SetColumn(element, coord[2]);

                if (move.Length > 4)
                    MoveOpponenet(move.Substring(4, move.Length - 4));
            }

            if (_match.CheckMate())
            {
                imgDefeat.Visibility = Visibility.Visible;
                _eom = true;
                _turn = false;
                _networkSslStream.Write(Encoding.UTF8.GetBytes("DSC/" + _stanza + "/Bravo, scacco matto, hai vinto!/Vittoria!"));
                MessageBox.Show("L'avversario ha vinto facendoti scacco matto!", "Sconfitta");
                return;
            }

            appo = _match.Draw();
            if (appo != String.Empty)
            {
                imgDraw.Visibility = Visibility.Visible;
                _eom = true;
                _turn = false;
                _networkSslStream.Write(Encoding.UTF8.GetBytes(appo + "/" + _stanza));
                MessageBox.Show("La partita è finita pari per impossibilità di fare scacco matto usando i pezzi rimanenti", "Pareggio", MessageBoxButton.OK);
                return;
            }
                
            _turn = true;
        }
        // mossa ricevuta dal server da parte dell'avversario

        private void AskForDraw(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Sei sicuro di voler chiedere la patta?", "Attenzione", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                _networkSslStream.Write(Encoding.UTF8.GetBytes("DRR/" + _stanza));
        }

        private void DeclareDefeat(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Sei sicuro di voler arrenderti? Se confermi perderai automaticamente la partita!", "Attenzione", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                _networkSslStream.Write(Encoding.UTF8.GetBytes("DSC/" + _stanza + "/L'avversario si è arreso, hai vinto la partita/Vittoria!"));
                Canvas_ModalitaOnline.Visibility = Visibility.Hidden;
                Canvas_Menu.Visibility = Visibility.Visible;
            }            
        }
        #endregion

        #region CANVAS 5 PROFILO
        private void InitalizeCanvasProfilo(object sender, DependencyPropertyChangedEventArgs e)
        {
            GridStatistiche.Visibility = Visibility.Visible;
            GridCambiaPassword.Visibility = Visibility.Hidden;
            GridCambiaAvatar.Visibility = Visibility.Hidden;
            lblProfiloUsername.Content = lblUsername.Content;
            lblProfiloError.Content = String.Empty;
            lblProfiloError.Background = Brushes.Transparent;

            if (_profiloFunctions)
            {
                btnCambiaAvatar.Visibility = Visibility.Visible;
                btnCambiaPassword.Visibility = Visibility.Visible;
                btnStatistiche.Visibility = Visibility.Visible;
            }
            else
            {
                btnCambiaAvatar.Visibility = Visibility.Hidden;
                btnCambiaPassword.Visibility = Visibility.Hidden;
                btnStatistiche.Visibility = Visibility.Hidden;
            }
        }

        private void CambiaAvatar(object sender, RoutedEventArgs e)
        {
            GridStatistiche.Visibility = Visibility.Hidden;
            GridCambiaPassword.Visibility = Visibility.Hidden;
            GridCambiaAvatar.Visibility = Visibility.Visible;
        }

        private void CambiaPassword(object sender, RoutedEventArgs e)
        {
            GridStatistiche.Visibility = Visibility.Hidden;
            GridCambiaPassword.Visibility = Visibility.Visible;
            GridCambiaAvatar.Visibility = Visibility.Hidden;
            lblCambiaPasswordError.Content = String.Empty;
            txtCambiaPassword.Visibility = Visibility.Visible;
            txtCambiaPasswordConfirm.Visibility = Visibility.Visible;
            btnShowCambiaPassword.Visibility = Visibility.Hidden;
            btnShowCambiaPasswordConfirm.Visibility = Visibility.Hidden;
            txtCambiaPassword.Foreground = Brushes.White;
            txtCambiaPassword.Text = "Nuova Password";
            txtCambiaPasswordConfirm.Foreground = Brushes.White;
            txtCambiaPasswordConfirm.Text = "Cambia Password";
            pswCambiaPassword.Password = String.Empty;
            pswCambiaPasswordConfirm.Password = String.Empty;
        }

        private void VisualizzaStatistiche(object sender, RoutedEventArgs e)
        {
            GridStatistiche.Visibility = Visibility.Visible;
            GridCambiaPassword.Visibility = Visibility.Hidden;
            GridCambiaAvatar.Visibility = Visibility.Hidden;
        }

        private void CambiaPasswordTastiera(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
                AvviaCambioPassword();
        }

        private void CambiaPasswordPulsante(object sender, RoutedEventArgs e)
        {
            AvviaCambioPassword();   
        }

        private void AvviaCambioPassword()
        {
            try
            {
                if (txtCambiaPassword.Visibility == Visibility.Visible)
                {
                    if (txtCambiaPassword.Text.Contains("/")) throw new Exception(String.Format("La password non può contenere il carattere '/'"));
                }
                else if (pswCambiaPassword.Password.Contains("/")) throw new Exception(String.Format("La password non può contenere il carattere '/'"));

                if (txtCambiaPassword.Foreground == Brushes.Gray || txtCambiaPassword.Text.Trim() == String.Empty) throw new Exception(String.Format("La password non può essere vuota"));
                if (txtCambiaPasswordConfirm.Foreground == Brushes.Gray || txtCambiaPasswordConfirm.Text.Trim() == String.Empty) throw new Exception(String.Format("Le password non coincidono"));
                if (pswCambiaPassword.Foreground == Brushes.Gray || pswCambiaPassword.Password.Trim() == String.Empty) throw new Exception(String.Format("La password non può essere vuota"));
                if (pswCambiaPasswordConfirm.Foreground == Brushes.Gray || pswCambiaPasswordConfirm.Password.Trim() == String.Empty) throw new Exception(String.Format("Le password non coincidono"));

                int caso = 0;
                if (txtCambiaPassword.Visibility == Visibility.Visible && txtCambiaPasswordConfirm.Visibility == Visibility.Hidden) caso = 1;
                if (txtCambiaPassword.Visibility == Visibility.Hidden && txtCambiaPasswordConfirm.Visibility == Visibility.Visible) caso = 2;
                if (txtCambiaPassword.Visibility == Visibility.Hidden && txtCambiaPasswordConfirm.Visibility == Visibility.Hidden) caso = 3;
                switch (caso)
                {
                    case 0:
                        if (txtCambiaPassword.Text.Trim() != txtCambiaPasswordConfirm.Text.Trim()) throw new Exception(String.Format("Le password non coincidono"));
                        break;
                    case 1:
                        if (txtCambiaPassword.Text.Trim() != pswCambiaPasswordConfirm.Password.Trim()) throw new Exception(String.Format("Le password non coincidono"));
                        break;
                    case 2:
                        if (pswCambiaPassword.Password.Trim() != txtCambiaPassword.Text.Trim()) throw new Exception(String.Format("Le password non coincidono"));
                        break;
                    case 3:
                        if (pswCambiaPassword.Password.Trim() != pswCambiaPasswordConfirm.Password.Trim()) throw new Exception(String.Format("Le password non coincidono"));
                        break;
                }

                byte[] dataTx = Encoding.UTF8.GetBytes("CPS/" + lblUsername.Content.ToString().Trim() + "/" + pswCambiaPassword.Password.Trim());

                try
                {
                    _networkSslStream.Write(dataTx);
                }
                catch
                {
                    TryToConnectChessServer();
                    lblCambiaPasswordError.Content = "Errore di connessione, server irraggiungibile";
                }

            }
            catch (Exception ex)
            {
                lblCambiaPasswordError.Content = ex.Message.ToString();
            }
        }

        private void TextFocusProfilo(object sender, RoutedEventArgs e)
        {
            TextBox t = (TextBox)sender;
            string name = t.Name;
            PasswordBox[] p = { pswCambiaPassword, pswCambiaPasswordConfirm };
            Button[] b = { btnShowCambiaPassword, btnShowCambiaPasswordConfirm };

            if (t.Foreground == Brushes.White)
            {
                t.Text = String.Empty;
                t.Foreground = Brushes.LightYellow;
                int x = 0;
                if (name.Contains("Confirm")) x = 1;
                t.Visibility = Visibility.Hidden;
                t.Text = "x";
                p[x].Visibility = Visibility.Visible;
                p[x].Focus();
                b[x].Visibility = Visibility.Visible;
            }
        }

        private void TextLostFocusProfilo(object sender, RoutedEventArgs e)
        {
            TextBox t = (TextBox)sender;
            string name = t.Name;

            if (t.Text.Trim() == String.Empty)
            {
                t.Foreground = Brushes.Gray;
                switch (name)
                {
                    case "txtCambiaPassword":
                        t.Text = "Nuova Passowrd";
                        break;
                    case "txtCambiaPasswordConfirm":
                        t.Text = "Conferma Password";
                        break;
                }
            }
        }

        private void PasswordLostFocusProfilo(object sender, RoutedEventArgs e)
        {
            PasswordBox p = (PasswordBox)sender;
            string name = p.Name;
            TextBox[] t = { txtCambiaPassword, txtCambiaPasswordConfirm };
            Button[] b = { btnShowCambiaPassword, btnShowCambiaPasswordConfirm };

            if (p.Password.Trim() == String.Empty)
            {
                int x = 0;
                if (name.Contains("Confirm")) x = 1;
                t[x].Visibility = Visibility.Visible;
                p.Visibility = Visibility.Hidden;
                b[x].Visibility = Visibility.Hidden;
                t[x].Foreground = Brushes.White;
                if (x == 0) t[x].Text = "Nuova Password";
                else t[x].Text = "Conferma Password";
            }
        }

        private void ShowCambiaPassword(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            PasswordBox[] p = { pswCambiaPassword, pswCambiaPasswordConfirm };
            TextBox[] t = { txtCambiaPassword, txtCambiaPasswordConfirm };
            int x = 0;
            if (b.Name.Contains("Confirm")) x = 1;

            if (t[x].Visibility == Visibility.Visible)
            {
                p[x].Password = t[x].Text.Trim();
                p[x].Visibility = Visibility.Visible;
                t[x].Visibility = Visibility.Hidden;
            }
            else
            {
                t[x].Text = p[x].Password.Trim();
                p[x].Visibility = Visibility.Hidden;
                t[x].Visibility = Visibility.Visible;
            }
        }

        private void ChooseAvatar(object sender, MouseButtonEventArgs e)
        {
            var point = Mouse.GetPosition(GridCambiaAvatar);
            int x = 0;
            int y = 0;
            double accumulatedWidth = 0.0;
            double accumulatedHeight = 0.0;

            Image[,] avatarList = {
                { avatar0, avatar1, avatar2, avatar3, avatar4, avatar5 },
                { avatar6, avatar7, avatar8, avatar9, avatar10, avatar11},
                { avatar12, avatar13, avatar14, avatar15, avatar16, null} };


            // calcolo il numero della colonna
            foreach (var columnDefinition in GridCambiaAvatar.ColumnDefinitions)
            {
                accumulatedWidth += columnDefinition.ActualWidth;
                if (accumulatedWidth >= point.X)
                    break;
                x++;
            }
            foreach (var rowDefinition in GridCambiaAvatar.RowDefinitions)
            {
                accumulatedHeight += rowDefinition.ActualHeight;
                if (accumulatedHeight >= point.Y)
                    break;
                y++;
            }

            _networkSslStream.Write(Encoding.UTF8.GetBytes("CAV/" + lblUsername.Content.ToString().Trim() + "/" + avatarList[y, x].Source));
        }
        #endregion

        #region CANVAS 6 AMICI
        private void InitalizeCanvasGiocaConAmici(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
            {
                btnRichiediAmicizia.Visibility = Visibility.Hidden;
                btnAccettaAmici.Visibility = Visibility.Hidden;
                btnRifiutaAmici.Visibility = Visibility.Hidden;
                btnStartMatchAmici.Visibility = Visibility.Visible;

                lblErrorGiocaConAmici2.Visibility = Visibility.Hidden;
                lblErrorGiocaConAmici.Visibility = Visibility.Visible;
                lstAmici.Visibility = Visibility.Hidden;
                imgNotifica.Visibility = Visibility.Hidden;
                lstAmici.Items.Clear();
                txtCercaAmici.Text = "Cerca amici";
                lblListName.Content = "Amici";
                txtCercaAmici.Foreground = Brushes.Black;
                _networkSslStream.Write(Encoding.UTF8.GetBytes("AMC/" + lblUsername.Content.ToString().Trim()));
            }
        }

        private void CercaAmiciBottone(object sender, RoutedEventArgs e)
        {

            if (!(txtCercaAmici.Text == String.Empty || txtCercaAmici.Foreground == Brushes.Black))
            {
                _networkSslStream.Write(Encoding.UTF8.GetBytes("SRC/" + txtCercaAmici.Text.Trim() + "/" + _username));
                lblListName.Content = "Risultati di ricerca";

                btnRichiediAmicizia.Visibility = Visibility.Visible;
                btnAccettaAmici.Visibility = Visibility.Hidden;
                btnRifiutaAmici.Visibility = Visibility.Hidden;
                btnStartMatchAmici.Visibility = Visibility.Hidden;
            }
        }

        private void RichiestaAmiciziaBottone(object sender, RoutedEventArgs e)
        {
            imgNotifica.Visibility = Visibility.Hidden;
            lblListName.Content = "Richieste d'amicizia";

            btnRichiediAmicizia.Visibility = Visibility.Hidden;
            btnAccettaAmici.Visibility = Visibility.Visible;
            btnRifiutaAmici.Visibility = Visibility.Visible;
            btnStartMatchAmici.Visibility = Visibility.Hidden;

            _networkSslStream.Write(Encoding.UTF8.GetBytes("AMR/" + _username));
        }

        private void BarraCercaAmiciGotFocus(object sender, RoutedEventArgs e)
        {
            txtCercaAmici.Text = String.Empty;
            txtCercaAmici.Foreground = lblErrorGiocaConAmici.Foreground; 
        }

        private void BarraCercaAmiciLostFocus(object sender, RoutedEventArgs e)
        {
            if(txtCercaAmici.Text == String.Empty)
            {
                txtCercaAmici.Text = "Cerca amici";
                txtCercaAmici.Foreground = Brushes.Black;
            }
        }

        private void BarraCercaAmiciInvioDaTastiera(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                if (!(txtCercaAmici.Text == String.Empty || txtCercaAmici.Foreground == Brushes.Black))
                {
                    _networkSslStream.Write(Encoding.UTF8.GetBytes("SRC/" + txtCercaAmici.Text.Trim() + "/" + _username));
                    lblListName.Content = "Risultati di ricerca";

                    btnRichiediAmicizia.Visibility = Visibility.Visible;
                    btnAccettaAmici.Visibility = Visibility.Hidden;
                    btnRifiutaAmici.Visibility = Visibility.Hidden;
                    btnStartMatchAmici.Visibility = Visibility.Hidden;
                }          
            }
        }

        private void btnBackToAmiciClick(object sender, RoutedEventArgs e)
        {
            _networkSslStream.Write(Encoding.UTF8.GetBytes("AMC/" + lblUsername.Content.ToString().Trim()));
            lblListName.Content = "Amici";

            btnRichiediAmicizia.Visibility = Visibility.Hidden;
            btnAccettaAmici.Visibility = Visibility.Hidden;
            btnRifiutaAmici.Visibility = Visibility.Hidden;
            btnStartMatchAmici.Visibility = Visibility.Visible;
        }

        private void InviaRichiestaAmicizia(object sender, RoutedEventArgs e)
        {
            lblErrorGiocaConAmici2.Visibility = Visibility.Visible;

            if (lstAmici.SelectedItem != null)  
                _networkSslStream.Write(Encoding.UTF8.GetBytes("SFR/" + _username + "/" + lstAmici.SelectedValue.ToString()));
            else lblErrorGiocaConAmici2.Content = "Selezionare un giocatore";

        }

        private void AccettaRichiestaAmicizia(object sender, RoutedEventArgs e)
        {
            lblErrorGiocaConAmici2.Visibility = Visibility.Visible;

            if (lstAmici.SelectedItem != null)
                _networkSslStream.Write(Encoding.UTF8.GetBytes("AFR/" + _username + "/" + lstAmici.SelectedValue.ToString()));
            else lblErrorGiocaConAmici2.Content = "Selezionare un giocatore";
        }

        private void RifiutaRichiestaAmicizia(object sender, RoutedEventArgs e)
        {
            lblErrorGiocaConAmici2.Visibility = Visibility.Visible;

            if (lstAmici.SelectedItem != null)
                _networkSslStream.Write(Encoding.UTF8.GetBytes("DFR/" + _username + "/" + lstAmici.SelectedValue.ToString()));
            else lblErrorGiocaConAmici2.Content = "Selezionare un giocatore";
        }

        private void InviaRichiestaDiGioco(object sender, RoutedEventArgs e)
        {
            if (lstAmici.SelectedValue.ToString().Substring(100, lstAmici.SelectedValue.ToString().Length - 100).Trim() == "Nel menu" && lstAmici.SelectedValue != null)
                _networkSslStream.Write(Encoding.UTF8.GetBytes("SMR/" + _username + "/" + lstAmici.SelectedValue.ToString().Substring(0, lstAmici.SelectedValue.ToString().IndexOf(" ")).Trim() + "/ask"));
        }
        #endregion

        #region CANVAS 7 CLASSIFICHE
        private void InitalizeCanvasClassifiche(object sender, DependencyPropertyChangedEventArgs e)
        {
            lstClassifiche.Items.Clear();
            _networkSslStream.Write(Encoding.UTF8.GetBytes("CLA/" + _username));
        }
        #endregion

        #region CANVAS 8 REPLAY
        private void InitalizeCanvasReplay(object sender, DependencyPropertyChangedEventArgs e)
        {
            lstReplay.Items.Clear();
            _networkSslStream.Write(Encoding.UTF8.GetBytes("REP/all"));
        }

        private void VisualizzaReplay(object sender, MouseButtonEventArgs e)
        {
            string date;
            string bianco;
            string nero;
            string info = lstReplay.SelectedValue.ToString();

            date = info.Substring(0, info.IndexOf(':') + 6);
            info = info.Substring(info.IndexOf(':') + 6, info.Length - (info.IndexOf(':') + 6)).Trim();

            bianco = info.Substring(0, info.IndexOf(','));
            info = info.Substring(info.IndexOf(',') + 5, info.Length - (info.IndexOf(',') + 5)).Trim();

            nero = info.Substring(0, info.IndexOf(','));

            _networkSslStream.Write(Encoding.UTF8.GetBytes("SHR/" + date + "/" + bianco + "/" + nero));
        }
        #endregion

        #region CANVAS 9 REPLAYVIEW

        private void InitalizeCanvasViewReplay(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue == true)
                InitalizeScacchieraReplay();
        }

        private void ReplayGoNext(object sender, RoutedEventArgs e)
        {
            if(_replayIndex * 4 < _replayMoves.Length)
            {
                string appo = String.Empty;
                string numeri = "1234567890";

                // UTF8 7 = 48, 6 = 49, 5 = 50 ...
                int[] coord = {
                    Convert.ToInt32(_replayMoves[_replayIndex * 4]) - 55,
                    Convert.ToInt32(_replayMoves[(_replayIndex * 4) + 1]) - 55,
                    Convert.ToInt32(_replayMoves[(_replayIndex * 4) + 2]) - 55,
                    Convert.ToInt32(_replayMoves[(_replayIndex * 4) + 3]) - 55
                };

                for (int i = 0; i < 4; i++)
                {
                    if (coord[i] < 0) coord[i] *= -1;
                    coord[i] = 7 - coord[i];
                }

                if (_turn)
                {
                    for (int i = 0; i < 4; i++)
                        coord[i] = 7 - coord[i];
                }

                _turn = !_turn;

                UIElement element = new UIElement();
                Image addImage = new Image();

                // in caso di fondo scacchiera arriva un messaggio speciale composto da un non spostamento
                if (_replayMoves.Substring(_replayIndex * 4, 2) == _replayMoves.Substring((_replayIndex * 4) + 2, 2))
                {
                    if (coord[3] == 1) coord[3] = 0;
                    if (coord[3] == 6) coord[3] = 7;

                    element = ScacchieraReplay.Children.Cast<UIElement>().FirstOrDefault(ex => Grid.GetColumn(ex) == coord[2] && Grid.GetRow(ex) == coord[3]);
                    ScacchieraReplay.Children.Remove(element);

                    switch (_replayMoves.Substring(_replayIndex * 4 + 4, 1))
                    {
                        case "t":
                            if (_replayMoves.Substring(_replayIndex * 4 + 5, 2).Contains("N")) addImage.Source = tNsx.Source;
                            else addImage.Source = tBsx.Source;
                            _replay.UpgradePiece(coord[2], coord[3], 21);
                            break;
                        case "c":
                            if (_replayMoves.Substring(_replayIndex * 4 + 5, 2).Contains("N")) addImage.Source = cNsx.Source;
                            else addImage.Source = cBsx.Source;
                            _replay.UpgradePiece(coord[2], coord[3], 22);
                            break;
                        case "a":
                            if (_replayMoves.Substring(_replayIndex * 4 + 5, 2).Contains("N")) addImage.Source = aNsx.Source;
                            else addImage.Source = aBsx.Source;
                            _replay.UpgradePiece(coord[2], coord[3], 23);
                            break;
                        case "r":
                            if (_replayMoves.Substring(_replayIndex * 4 + 5, 2).Contains("N")) addImage.Source = rgN.Source;
                            else addImage.Source = rgB.Source;
                            _replay.UpgradePiece(coord[2], coord[3], 24);
                            break;
                    }

                    addImage.Height = 80;
                    addImage.Width = 80;
                    ScacchieraReplay.Children.Add(addImage);
                    Grid.SetRow(addImage, coord[3]);
                    Grid.SetColumn(addImage, coord[2]);
                }
                else
                {
                    if (_replay.MoveOpponent(coord[0], coord[1], coord[2], coord[3]))
                    {
                        element = ScacchieraReplay.Children.Cast<UIElement>().FirstOrDefault(ex => Grid.GetColumn(ex) == coord[2] && Grid.GetRow(ex) == coord[3]);
                        ScacchieraReplay.Children.Remove(element);
                    }
                    element = ScacchieraReplay.Children.Cast<UIElement>().FirstOrDefault(ex => Grid.GetColumn(ex) == coord[0] && Grid.GetRow(ex) == coord[1]);
                    Grid.SetRow(element, coord[3]);
                    Grid.SetColumn(element, coord[2]);

                    if (_replayIndex * 4 + 4 != _replayMoves.Length)
                    {
                        if (_replayMoves.Substring(_replayIndex * 4 + 2, 2) == _replayMoves.Substring(_replayIndex * 4 + 4, 2))
                        {
                            _replayMoves = _replayMoves.Remove(_replayIndex * 4, 4);
                            _turn = !_turn;
                            ReplayGoNext(btnGoNextMove, new RoutedEventArgs());
                            _replayIndex--;
                        }

                        if (_replayMoves.Substring(_replayIndex * 4 + 4, 2) == _replayMoves.Substring(_replayIndex * 4 + 6, 2))
                        {
                            _replayIndex++;
                            _turn = !_turn;
                            ReplayGoNext(btnGoNextMove, new RoutedEventArgs());
                            _replayIndex--;
                        }
                    }
                }

                _replayIndex++;

                if (_replayIndex != _replayMoves.Length)
                {
                    while (numeri.IndexOf(_replayMoves[_replayIndex * 4]) < 0)
                    {
                        _replayMoves = _replayMoves.Remove(_replayIndex * 4, 1);
                        if (numeri.IndexOf(_replayMoves[_replayIndex * 4]) >= 0)
                            _replayMoves = _replayMoves.Remove(_replayIndex * 4, 1);
                    }
                } 
            }
        }

        private void ReplayGoBack(object sender, RoutedEventArgs e)
        {
            if(_replayIndex > 0)
            {
                _replayIndex--;
                _replayMoves = _replayBackUp;
                InitalizeScacchieraReplay();
                int appo = _replayIndex;
                _replayIndex = 0;
                while (_replayIndex < appo)
                    ReplayGoNext(btnGoNextMove, new RoutedEventArgs());
            }
        }

        private void InitalizeScacchieraReplay()
        {
            Image[,] pieces = {
                    { tNsxR, cNsxR, aNsxR, rgNR, reNR, aNdxR, cNdxR, tNdxR, pN1R, pN2R, pN3R, pN4R, pN5R, pN6R, pN7R, pN8R },
                    { tBsxR, cBsxR, aBsxR, rgBR, reBR, aBdxR, cBdxR, tBdxR, pB1R, pB2R, pB3R, pB4R, pB5R, pB6R, pB7R, pB8R }
                };

            ScacchieraReplay.Children.RemoveRange(0, ScacchieraReplay.Children.Count);

            foreach (Image img in pieces)
            {
                ScacchieraReplay.Children.Add(img);
            }

            for (int i = 0; i < 2; i++)
            {
                for (int x = 0; x < 16; x++)
                {
                    if (x < 8)
                        pieces[i, x].SetValue(Grid.RowProperty, Math.Abs(i * 7));
                    else
                    {
                        if (i == 0)
                            pieces[i, x].SetValue(Grid.RowProperty, Math.Abs(i * 7) + 1);
                        else
                            pieces[i, x].SetValue(Grid.RowProperty, Math.Abs(i * 7) - 1);
                    }

                    pieces[i, x].SetValue(Grid.ColumnProperty, x % 8);
                }
            }

            _turn = true;
            _counterPromotions = 0;
        }
        #endregion

        #region GESTORE PROTOCOLLO
        private void gestoreEventiStart(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                Thread.Sleep(1000000000);
            }
        }

        private void gestoreEventiChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == -1)
            {
                int p;
                string textRx = Encoding.UTF8.GetString(_buffer, 0, _buffer.Length);
                textRx = textRx.Substring(0, textRx.IndexOf('\u0000'));
                string[] fields = textRx.Split('/');

                switch (fields[0])
                {
                    case "LGY":
                        _username = txtUser.Text.Trim();
                        lblUsername.Content = _username;
                        Canvas_Log.Visibility = Visibility.Hidden;
                        Canvas_Menu.Visibility = Visibility.Visible;
                        break;
                    case "LGN":
                        lblError.Content = fields[1];
                        break;
                    case "RGY":
                        Return(btnReturn, new RoutedEventArgs());
                        break;
                    case "RGN":
                        lblError.Content = fields[1];
                        break;
                    case "FND":
                        lblInfo1_1.Content = fields[1];

                        _eom = false;
                        _turn = false;
                        if (Convert.ToInt16(fields[2]) == -2)
                            _turn = true;

                        _stanza = fields[3];
                        _match = new Match(_turn);
                        _loading.CancelAsync();

                        Canvas_ModalitaOnline.Visibility = Visibility.Visible;
                        Canvas_Matchmaking.Visibility = Visibility.Hidden;
                        Canvas_Menu.Visibility = Visibility.Hidden;
                        Canvas_GiocaConAmici.Visibility = Visibility.Hidden;
                        Canvas_Classifiche.Visibility = Visibility.Hidden;
                        Canvas_Profilo.Visibility = Visibility.Hidden;
                        Canvas_Replay.Visibility = Visibility.Hidden;
                        Canvas_ViewReplay.Visibility = Visibility.Hidden;

                        break;
                    case "MSG":
                        PrintChatMessage(fields[1], fields[2], false);
                        break;
                    case "DSC":
                        imgWin.Visibility = Visibility.Visible;
                        _eom = true;
                        _turn = false;
                        MessageBox.Show(String.Format(fields[1], fields[2]));                                
                        break;
                    case "MOV":
                        MoveOpponenet(fields[1]);
                        break;
                    case "DRR":
                        string response;
                        if (MessageBox.Show("L'avversario chiede la patta, accetti?", "Richiesta di patta", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        {
                            response = "DRA/";
                            imgDraw.Visibility = Visibility.Visible;
                            _eom = true;
                            _turn = false;
                        }
                        else
                            response = "DRD/";

                        response += _stanza;

                        _networkSslStream.Write(Encoding.UTF8.GetBytes(response));
                        break;
                    case "DRA":
                        imgDraw.Visibility = Visibility.Visible;
                        _eom = true;
                        _turn = false;
                        MessageBox.Show("L'avversario ha accettato di fare patta", "Richiesta accettata", MessageBoxButton.OK);
                        break;
                    case "DRD":
                        MessageBox.Show("L'avversario ha rifiutato la rischiesta di patta", "Richiesta rifiutata", MessageBoxButton.OK);
                        break;
                    case "DRP":
                        imgDraw.Visibility = Visibility.Visible;
                        _eom = true;
                        _turn = false;
                        MessageBox.Show("La partita è finita pari per impossibilità di fare scacco matto usando i pezzi rimanenti", "Pareggio", MessageBoxButton.OK);
                        break;
                    case "CPN":
                    case "CPY":
                        lblCambiaPasswordError.Content = fields[1];
                        break;
                    case "PRO":
                        imgAvatar.Source = new BitmapImage(new Uri(@"pack://application:,,,/img/avatar/" + fields[6]));
                        imgAvatarProfilo.Source = imgAvatar.Source;
                        try {
                            lblPunteggio.Content = fields[7];
                            lblProfiloPunteggio.Content = lblPunteggio.Content;                         
                            lblPartiteVinte.Content = Convert.ToInt32(fields[8]);
                            lblPartitePareggiate.Content = Convert.ToInt32(fields[9]);
                            lblPartitePerse.Content = Convert.ToInt32(fields[10]);
                            lblPartiteGiocate.Content = (Convert.ToInt32(lblPartiteVinte.Content) + Convert.ToInt32(lblPartitePareggiate.Content) + Convert.ToInt32(lblPartitePerse.Content)).ToString();

                            if ((p = Convert.ToInt32(fields[7])) > 1000)
                            {
                                if (p > 1100) {
                                    if (p > 1300)
                                    {
                                        if (p > 1500)
                                        {
                                            if (p > 1800) {
                                                lblGrado.Content = "Leggenda";
                                            }
                                            else lblGrado.Content = "Grande Maestro";
                                        }
                                        else lblGrado.Content = "Campione";
                                    }
                                    else lblGrado.Content = "Esperto";
                                }                                    
                                else
                                    lblGrado.Content = "Dilettante";
                            }
                            else
                                lblGrado.Content = "Principiante";

                            lblProfiloGrado.Content = lblGrado.Content;
                        }
                        catch
                        {
                            lblGrado.Content = "errore";
                            lblProfiloGrado.Content = lblGrado.Content;
                            lblPunteggio.Content = "errore";
                            lblProfiloPunteggio.Content = lblPunteggio.Content;
                            lblPartiteGiocate.Content = "errore";
                            lblPartiteVinte.Content = "errore";
                            lblPartitePareggiate.Content = "errore";
                            lblPartitePerse.Content = "errore";
                        }
                        break;
                    case "CAY":
                        imgAvatar.Source = new BitmapImage(new Uri(@"pack://application:,,,/img/avatar/" + fields[7]));
                        imgAvatarProfilo.Source = imgAvatar.Source;
                        lblProfiloError.Background = Brushes.White;
                        lblProfiloError.Opacity = 0.9;
                        lblProfiloError.Content = fields[1];
                        break;
                    case "CAN":
                        lblProfiloError.Background = Brushes.White;
                        lblProfiloError.Opacity = 0.9;
                        lblProfiloError.Content = fields[1];
                        break;
                    case "OPP":
                        imgAvatarOpponent.Source = new BitmapImage(new Uri(@"pack://application:,,,/img/avatar/" + fields[6]));
                        lblPunteggioOpponent.Content = fields[7];

                        try
                        {
                            if ((p = Convert.ToInt32(fields[7])) > 1000)
                            {
                                if (p > 1100)
                                {
                                    if (p > 1300)
                                    {
                                        if (p > 1500)
                                        {
                                            if (p > 1800)
                                            {
                                                lblGradoOpponent.Content = "Leggenda";
                                            }
                                            else lblGradoOpponent.Content = "Grande Maestro";
                                        }
                                        else lblGradoOpponent.Content = "Campione";
                                    }
                                    else lblGradoOpponent.Content = "Esperto";
                                }
                                else
                                    lblGradoOpponent.Content = "Dilettante";
                            }
                            else
                                lblGradoOpponent.Content = "Principiante";
                        }
                        catch { lblGradoOpponent.Content = "errore"; }
                        break;

                    case "AMC":
                        String lstColumns = "{0, -50} {1, -50} {2}";

                        if (Convert.ToInt32(fields[1]) > 0) imgNotifica.Visibility = Visibility.Visible;
                        if (!(Convert.ToInt32(fields[2]) == 0 && Convert.ToInt32(fields[3]) == 0))
                        {
                            lstAmici.Items.Clear();
                            lblErrorGiocaConAmici.Visibility = Visibility.Hidden;
                            lstAmici.Visibility = Visibility.Visible;

                            int x = 3;
                            int index = 3;
                            int amici = Convert.ToInt32(fields[2]);
                            for (; x < index + (3 * amici); x++)
                                lstAmici.Items.Add(String.Format(lstColumns, fields[x], fields[++x], fields[++x]));
                            index += (3 * amici);
                            amici = Convert.ToInt32(fields[3 + (3 * amici)]);
                            x++;
                            for (; x < index + (3 * amici); x++)
                                lstAmici.Items.Add(String.Format(lstColumns, fields[x], fields[++x], fields[++x]));
                        }                         
                        break;
                    case "SRC":
                        lstAmici.Items.Clear();
                        for(int i = 2; i < (2 + Convert.ToInt32(fields[1])); i++)
                            lstAmici.Items.Add(fields[i]);

                        if (Convert.ToInt32(fields[1]) == 0)
                        {
                            lstAmici.Visibility = Visibility.Hidden;
                            lblErrorGiocaConAmici.Visibility = Visibility.Visible;
                            lblErrorGiocaConAmici.Content = "La ricerca non ha prodotto risultati";
                        }
                        else {
                            lstAmici.Visibility = Visibility.Visible;
                            lblErrorGiocaConAmici.Visibility = Visibility.Hidden;
                        }

                        break;
                    case "AMR":
                        lstAmici.Items.Clear();

                        for (int i = 2; i < 2 + Convert.ToInt32(fields[1]); i++)
                            lstAmici.Items.Add(fields[i]);

                        if (Convert.ToInt32(fields[1]) == 0)
                        {
                            lblErrorGiocaConAmici.Content = "Nessuna richiesta di amicizia al momento";
                            lblErrorGiocaConAmici.Visibility = Visibility.Visible;
                            lstAmici.Visibility = Visibility.Hidden;
                        }
                        else
                        {
                            lblErrorGiocaConAmici.Visibility = Visibility.Hidden;
                            lstAmici.Visibility = Visibility.Visible;
                        }

                        break;
                    case "CLA":
                        String lstColumnsClassifiche = "{0, -50} {1, -50} {2}";
                        string grado = String.Empty;

                        lstClassifiche.Items.Clear();

                        for (int i = 2; i < 2 + Convert.ToInt32(fields[1]); i++)
                        {
                            if ((p = Convert.ToInt32(fields[i + 1])) > 1000)
                            {
                                if (p > 1100)
                                {
                                    if (p > 1300)
                                    {
                                        if (p > 1500)
                                        {
                                            if (p > 1800)
                                            {
                                                grado = "Leggenda";
                                            }
                                            else grado = "Grande Maestro";
                                        }
                                        else grado = "Campione";
                                    }
                                    else grado = "Esperto";
                                }
                                else
                                    grado = "Dilettante";
                            }
                            else
                                grado = "Principiante";

                            lstClassifiche.Items.Add(String.Format(lstColumnsClassifiche, fields[i], fields[++i], grado));
                        }

                        break;
                    case "SFR":
                        switch(fields[1])
                        {
                            case "ok":
                                lblErrorGiocaConAmici2.Content = "Richiesta inoltrata";
                                break;
                            case "true":
                                lblErrorGiocaConAmici2.Content = "Siete già amici";
                                break;
                            case "false":
                                lblErrorGiocaConAmici2.Content = "Amicizia rifiutata";
                                break;
                            case "":
                                lblErrorGiocaConAmici2.Content = "Richiesta già effettuata";
                                break;
                        }
                        break;
                    case "AFR":
                    case "DFR":
                        switch (fields[1])
                        {
                            case "ok":
                                if(fields[0] == "AFR") lblErrorGiocaConAmici2.Content = "Ora siete amici";
                                else lblErrorGiocaConAmici2.Content = "Richiesta rifiutata";
                                _networkSslStream.Write(Encoding.UTF8.GetBytes("AMR/" + _username));
                                break;
                            default:
                                MessageBox.Show(fields[1], "Errore");
                                break;
                        }
                        break;
                    case "SMR":
                        switch (fields[1])
                        {
                            case "error":
                                lblErrorGiocaConAmici2.Visibility = Visibility.Visible;
                                lblErrorGiocaConAmici2.Content = "Giocatore non disponibile";
                                break;
                            case "no":
                                lblErrorGiocaConAmici2.Visibility = Visibility.Visible;
                                lblErrorGiocaConAmici2.Content = "Invito rifiutato";
                                break;
                            case "ask":
                                if (MessageBox.Show(fields[2] + " ti ha appena invitato a giocare un'amichevole, accetti?", "Amichevole", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                                    _networkSslStream.Write(Encoding.UTF8.GetBytes("SMR/" + _username + "/" + fields[2] + "/yes"));
                                else
                                    _networkSslStream.Write(Encoding.UTF8.GetBytes("SMR/" + _username + "/" + fields[2] + "/no"));
                                break;
                        }
                        break;
                    case "REP":

                        String lstColumnsReplay = "{0, -30} {1, -50} {2}";

                        lstReplay.Items.Clear();
                        int fine = fields.Count();
                        for (int x = 1; x < fine; x++) 
                            lstReplay.Items.Add(String.Format(lstColumnsReplay, fields[x], fields[++x] + "," + fields[++x], fields[++x] + "," + fields[++x]));

                        break;

                    case "SHR":
                        Canvas_Replay.Visibility = Visibility.Hidden;
                        Canvas_ViewReplay.Visibility = Visibility.Visible;

                        _replayMoves = fields[1];
                        _replayBackUp = _replayMoves;
                        _replayIndex = 0;

                        _replay = new Match();

                        break;
                }

                Array.Clear(_buffer, 0, _buffer.Length);
            }
        }

        private void ReceiveCallBack(IAsyncResult AR)
        {
            try
            {
                SslStream stream = (SslStream)AR.AsyncState;

                int received = stream.EndRead(AR);

                // Testo ricevuto
                string textRx = Encoding.UTF8.GetString(_buffer, 0, received);
                string[] fields = textRx.Split('/');

                if (fields[0] != String.Empty)
                    _gestoreEventi.ReportProgress(-1);

                stream.BeginRead(_buffer, 0, _buffer.Length, new AsyncCallback(ReceiveCallBack), stream);
            }
            catch { }
        }
        #endregion

        private void TryToConnectChessServer()
        {
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            const int portaServer = 70;
            const string nomeServer = "localhost";

            try
            {
                IPAddress[] indirizziIp = Array.FindAll(Dns.GetHostAddresses(nomeServer), a => a.AddressFamily == AddressFamily.InterNetwork);

                IPEndPoint serverEp = new IPEndPoint(indirizziIp[0], portaServer);
                clientSocket.Connect(serverEp);

                try
                {
                    _networkSslStream = new SslStream(new NetworkStream(clientSocket), false, ValidaCertificato);
                    _networkSslStream.AuthenticateAsClient(_nomeCertificatoServer);
                }
                catch (Exception e) { MessageBox.Show(e.Message.ToString(), "Mancata autenticazione del server"); Close(); }

                _networkSslStream.BeginRead(_buffer, 0, _buffer.Length, new AsyncCallback(ReceiveCallBack), _networkSslStream);
            }
            catch
            {
                lblError.Content = "Errore di connessione, server irraggiungibile";
            }
        }

        private void ReturnToMenu(object sender, RoutedEventArgs e)
        {
            if (Canvas_ModalitaOnline.Visibility == Visibility.Visible)
            {
                if (!_eom)
                {
                    if (MessageBox.Show("Sei sicuro di voler uscire? Se esci perderai automaticamente la partita!", "Attenzione", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                        _networkSslStream.Write(Encoding.UTF8.GetBytes("DSC/" + _stanza + "/L'avversario si è disconnesso, hai vinto la partita/Hai vinto!"));
                    else return;
                }

                Canvas_ModalitaOnline.Visibility = Visibility.Hidden;
                Canvas_Menu.Visibility = Visibility.Visible;
            }

            if (Canvas_Profilo.Visibility == Visibility.Visible)
            {
                Canvas_Profilo.Visibility = Visibility.Hidden;
                Canvas_Menu.Visibility = Visibility.Visible;
            }

            if (Canvas_GiocaConAmici.Visibility == Visibility.Visible)
            {
                Canvas_GiocaConAmici.Visibility = Visibility.Hidden;
                Canvas_Menu.Visibility = Visibility.Visible;
            }

            if (Canvas_Classifiche.Visibility == Visibility.Visible)
            {
                Canvas_Classifiche.Visibility = Visibility.Hidden;
                Canvas_Menu.Visibility = Visibility.Visible;
            }

            if (Canvas_Replay.Visibility == Visibility.Visible)
            {
                Canvas_Replay.Visibility = Visibility.Hidden;
                Canvas_Menu.Visibility = Visibility.Visible;
            }

            if(Canvas_ViewReplay.Visibility == Visibility.Visible)
            {
                Canvas_ViewReplay.Visibility = Visibility.Hidden;
                Canvas_Menu.Visibility = Visibility.Visible;
            }
        }

        private void VisualizzaProfiloAmico(object sender, MouseButtonEventArgs e)
        {
            string username;

            if (Canvas_Classifiche.Visibility == Visibility.Visible) username = lstClassifiche.SelectedValue.ToString().Substring(0, lstClassifiche.SelectedValue.ToString().IndexOf(" ")).Trim();
            else username = lstAmici.SelectedValue.ToString().Substring(0, lstAmici.SelectedValue.ToString().IndexOf(" ")).Trim();


            lblUsername.Content = username;
            _profiloFunctions = false;

            _networkSslStream.Write(Encoding.UTF8.GetBytes("PRO/" + username));

            Canvas_Profilo.Visibility = Visibility.Visible;
            Canvas_GiocaConAmici.Visibility = Visibility.Hidden;
            Canvas_Classifiche.Visibility = Visibility.Hidden;
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
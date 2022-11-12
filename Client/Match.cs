using System;
using System.Collections.Generic;

namespace Client
{
    public class Match
    {
        int _xRe = 4;
        int _yRe = 7;
        int _check = 0;
        int[,] _checkPieces = new int[2,2];
        int _arroccoL = 0;
        int _arroccoC = 7;
        int _enPassant = -1;
        public int[,] _pieces = new int[8, 8];
        List<int> _notDead = new List<int>();

        public Match() { InitializeChessboard(); }

        public Match(bool turno) {

            InitializeChessboard();

            if (!turno)
            {
                _pieces[0, 3] = 25;
                _pieces[0, 4] = 24;
                _pieces[7, 3] = 15;
                _pieces[7, 4] = 14;

                _xRe = 3;

                _arroccoC = 0;
                _arroccoL = 7;
            }
        }

        protected void InitializeChessboard()
        {

            //pezzi altrui
            _pieces[0, 0] = _pieces[0, 7] = 21;
            _pieces[0, 1] = _pieces[0, 6] = 22;
            _pieces[0, 2] = _pieces[0, 5] = 23;
            _pieces[0, 3] = 24;
            _pieces[0, 4] = 25;
            for (int i = 0; i < 8; i++) _pieces[1, i] = 26;

            // pezzi propri
            _pieces[7, 0] = _pieces[7, 7] = 11;
            _pieces[7, 1] = _pieces[7, 6] = 12;
            _pieces[7, 2] = _pieces[7, 5] = 13;
            _pieces[7, 3] = 14;
            _pieces[7, 4] = 15;
            for (int i = 0; i < 8; i++) _pieces[6, i] = 16;

            //campo neturo
            int x = 2;
            for (; x < 6; x++)
            {
                for (int i = 0; i < 8; i++)
                    _pieces[x, i] = 0;
            }

            #region CONTATORE_DI_PEZZI
            for(int i = 1; i < 4; i++)
            {
                for(int z = 0; z < 2; z++)
                {
                    _notDead.Add(10 + i);
                    _notDead.Add(20 + i);
                }                
            }

            _notDead.Add(14);
            _notDead.Add(24);

            for (int i = 0; i < 8; i++)
            {
                _notDead.Add(16);
                _notDead.Add(26);
            }
            #endregion
        }
        // inizizlizza l'array di numeri interi identificatori di ogni pezzo sulla tastiera

        public bool SelectCase(int x, int y)
        {
            // se la casella è piena viene selezionata per la mossa
            if ((_pieces[y, x] < 20 && 0 < _pieces[y, x]) || _pieces[y, x] > 30)
                return false;
            // se no si muove
            else
                return true;
        }

        public bool AllowMove(int oldX, int oldY, int newX, int newY, out int flagMove) {

            // id composto da un numero a due cifre:  
            // la prima cifra rappresenta il colore 1 = tuo, 2 = non tuo, 3 = non spostabile per via dello scacco
            // la seconda il pezzo:
            // 1 = torre
            // 2 = cavallo
            // 3 = alfiere
            // 4 = regina 
            // 5 = re
            // 6 = pedone
            List<int> allowed = new List<int>();
            int id = _pieces[oldY, oldX];
            bool[] direz = { true, true, true, true, true, true, true, true };
            flagMove = -1;

            // se il pezzo non è tuo
            if (id > 20 && id < 30) return false;

            if (id > 30)
                if (!(_xRe == oldX && oldX == newX) && !(_yRe == oldY && oldY == newY) && !(_yRe - _xRe == newY - newX && newY - newX == oldY - oldX) && !(_yRe + _xRe == newY + newX && newY + newX == oldY + oldX))
                    return false;

            // gestire re
            if (id == 15)
            {
                if (_check == 0)
                {
                    if (oldX == newX) { direz[2] = false; direz[6] = false; }
                    if (oldY == newY) { direz[0] = false; direz[4] = false; }
                    if (newY - newX == oldY - oldX) { direz[3] = false; direz[7] = false; }
                    if (newY + newX == oldY + oldX) { direz[1] = false; direz[5] = false; }
                }

                if (!(oldY - 1 <= newY && newY <= oldY + 1 && oldX - 1 <= newX && newX <= oldX + 1))
                {
                    if ((_arroccoC != -1 || _arroccoL != -1) && oldY == newY && _check == 0)
                    {
                        // tra i due or, prima l'arrocco a sinistra, dopo l' arrocco a destra
                        if (((_arroccoC == 0 || _arroccoL == 0) && newX == oldX - 2) || ((_arroccoC == 7 || _arroccoL == 7) && newX == oldX + 2))
                        {
                            int tempArroccoC = _arroccoC;
                            int tempArroccoL = _arroccoL;

                            if (oldX < newX)
                            {
                                for (int i = oldX + 1; i < 7; i++)
                                {
                                    if (_pieces[oldY, i] != 0) return false;
                                    if (!KingMove(direz, i, 7))
                                    {
                                        _arroccoC = tempArroccoC;
                                        _arroccoL = tempArroccoL;
                                        _check = 0;
                                        return false;
                                    }
                                }

                            }
                            if (oldX > newX)
                            {
                                for (int i = oldX - 1; i > 0; i--)
                                {
                                    if (_pieces[oldY, i] != 0) return false;
                                    if (!KingMove(direz, i, 7))
                                    {
                                        _arroccoC = tempArroccoC;
                                        _arroccoL = tempArroccoL;
                                        _check = 0;
                                        return false;
                                    }
                                }
                            }                            

                            ReleasePieces(direz, oldX, oldY);

                            _pieces[newY, newX] = 15;
                            _pieces[oldY, oldX] = 0;
                            _xRe = newX;
                            _yRe = newY;

                            UpdateStateOfPieces(direz);

                            int newXTorre;
                            if (newX < 3) newXTorre = newX + 1;
                            else newXTorre = newX - 1;
                 
                            if (newX == oldX + 2) { UpdateChessboard(7, 7, newXTorre, 7); flagMove = newXTorre; }
                            else { UpdateChessboard(0, 7, newXTorre, 7); flagMove = newXTorre; }

                            return true;

                        }
                        else return false;
                    }
                    else
                        return false;
                }

                if(KingMove(direz, newX, newY))
                {
                    // se li c'era una pedina avversaria
                    if (_pieces[newY, newX] != 0)
                        flagMove = 1;

                    ReleasePieces(direz, oldX, oldY);

                    _pieces[newY, newX] = 15;
                    _pieces[oldY, oldX] = 0;
                    _xRe = newX;
                    _yRe = newY;

                    UpdateStateOfPieces(direz);

                    return true;
                }
                else
                    return false;
            }

            // gestire mangiata del pedone
            if (id % 10 == 6)
            {
                if (oldY == newY + 1 && oldX == newX && _pieces[newY, newX] == 0) allowed.Add(6);
                if (oldY == newY + 1 && (newX == oldX - 1 || newX == oldX + 1)) {
                    if (_pieces[newY, newX] != 0) allowed.Add(6);
                    else if (newX == _enPassant && newY == 2)
                    {
                        direz = UpdateChessboard(oldX, oldY, newX, 3);
                        UpdateStateOfPieces(direz);
                        direz = UpdateChessboard(newX, 3, newX, 2);
                        UpdateStateOfPieces(direz);
                        flagMove = 6;
                        return true;
                    }
                } 
                if (oldY == 6 && oldY == newY + 2 && oldX == newX && _pieces[newY, newX] == 0) allowed.Add(6);
            }

            // verticali e orizzontali
            if (oldX == newX || oldY == newY)
            {
                if (oldY < newY)
                {
                    for (int i = oldY + 1; i < newY; i++)
                        if (_pieces[i, oldX] != 0) return false;
                }
                if (oldY > newY)
                {
                    for (int i = newY + 1; i < oldY; i++)
                        if (_pieces[i, oldX] != 0) return false;
                }
                if (oldX < newX)
                {
                    for (int i = oldX + 1; i < newX; i++)
                        if (_pieces[oldY, i] != 0) return false;
                }
                if (oldX > newX)
                {
                    for (int i = newX + 1; i < oldX; i++)
                        if (_pieces[oldY, i] != 0) return false;
                }

                allowed.Add(1); //torre
                allowed.Add(4); //regina

                // se torre ==> tolgo l'arrocco da quella torre
                if (id == 11)
                {
                    if (oldX == 0 && oldY == 7)
                    {
                        if (_arroccoC == 0) _arroccoC = -1;
                        else if (_arroccoL == 0) _arroccoL = -1;
                    }
                    if (oldX == 7 && oldY == 7)
                    {
                        if (_arroccoC == 7) _arroccoC = -1;
                        else if (_arroccoL == 7) _arroccoL = -1;
                    }
                }
            }

            // diagonali
            if (newY + newX == oldY + oldX || newY - newX == oldY - oldX)
            {
                int appoX, appoY;
                // alto a sinistra
                if (oldX > newX && oldY > newY)
                {
                    appoX = oldX - 1;
                    appoY = oldY - 1;
                    while (appoY > newY)
                        if (_pieces[appoY--, appoX--] != 0) return false;
                }
                // alto a destra
                if (oldX < newX && oldY > newY)
                {
                    appoX = oldX + 1;
                    appoY = oldY - 1;
                    while (appoY > newY)
                        if (_pieces[appoY--, appoX++] != 0) return false;
                }
                // basso a sinistra
                if (oldX > newX && oldY < newY)
                {
                    appoX = oldX - 1;
                    appoY = oldY + 1;
                    while (appoY < newY)
                        if (_pieces[appoY++, appoX--] != 0) return false;
                }
                // basso a sinistra
                if (oldX < newX && oldY < newY)
                {
                    appoX = oldX + 1;
                    appoY = oldY + 1;
                    while (appoY < newY)
                        if (_pieces[appoY++, appoX++] != 0) return false;
                }
                allowed.Add(4); //regina
                allowed.Add(3); //alfiere
            }

            // cavallo
            if (Math.Pow(newX - oldX, 2) + Math.Pow(newY - oldY, 2) - 5 == 0) allowed.Add(2);


            if (allowed.IndexOf(id % 10) < 0)
                return false;

            // caso di scacco 
            // se il programma arriva fino a qua significa che è stata mossa una pedina che non è il re
            if (_check > 0)
                if (TryToRemoveCheck(oldX, oldY, newX, newY)) return false;

            // se li c'era una pedina avversaria
            if (_pieces[newY, newX] != 0)
            {
                flagMove = 1;
                _notDead.Remove(20 + _pieces[newY, newX] % 10);
            }                

            direz = UpdateChessboard(oldX, oldY, newX, newY);
            UpdateStateOfPieces(direz);

            return true;
        }

        private bool KingMove(bool[] direz, int newX, int newY)
        {
            int addX = 0;
            int addY = 0;
            int currX;
            int currY;
            bool flag;
            int[] forbiddenPieces = new int[2];

            // in caso di pendoni che fanno scacco
            if (newY > 0)
            {
                if (newX > 0 && _pieces[newY - 1, newX - 1] == 26) return false;
                if (newX < 7 && _pieces[newY - 1, newX + 1] == 26) return false;
            }

            // in caso di re
            if (newY > 0)
            {
                if (newX > 0 && _pieces[newY - 1, newX - 1] == 25) return false;
                if (newX < 7 && _pieces[newY - 1, newX + 1] == 25) return false;
                if (_pieces[newY - 1, newX] == 25) return false;
            }
            if (newY < 7)
            {
                if (newX > 0 && _pieces[newY + 1, newX - 1] == 25) return false;
                if (newX < 7 && _pieces[newY + 1, newX + 1] == 25) return false;
                if (_pieces[newY + 1, newX] == 25) return false;
            }
            if (newX > 0 && _pieces[newY, newX - 1] == 25) return false;
            if (newX < 7 && _pieces[newY, newX + 1] == 25) return false;

            // in caso di cavalli che fanno scacco
            if (newY - 1 >= 0 && newX + 2 <= 7 && _pieces[newY - 1, newX + 2] == 22) return false;
            if (newY - 2 >= 0 && newX + 1 <= 7 && _pieces[newY - 2, newX + 1] == 22) return false;
            if (newY - 2 >= 0 && newX - 1 >= 0 && _pieces[newY - 2, newX - 1] == 22) return false;
            if (newY - 1 >= 0 && newX - 2 >= 0 && _pieces[newY - 1, newX - 2] == 22) return false;
            if (newY + 1 <= 7 && newX - 2 >= 0 && _pieces[newY + 1, newX - 2] == 22) return false;
            if (newY + 2 <= 7 && newX - 1 >= 0 && _pieces[newY + 2, newX - 1] == 22) return false;
            if (newY + 2 <= 7 && newX + 1 <= 7 && _pieces[newY + 2, newX + 1] == 22) return false;
            if (newY + 1 <= 7 && newX + 2 <= 7 && _pieces[newY + 1, newX + 2] == 22) return false; 

            for (int i = 0; i < 8; i++)
            {
                // se la direzione è quella giusta
                if (direz[i])
                {
                    addX = 0;
                    addY = 0;
                    currX = newX;
                    currY = newY;
                    flag = true;

                    // in base alla direzione si determina il valore da sommare alle coordinate ciascun controllo
                    if (i == 7 || i == 0 || i == 1) addX = 1;
                    if (i == 1 || i == 2 || i == 3) addY = -1;
                    if (i == 3 || i == 4 || i == 5) addX = -1;
                    if (i == 5 || i == 6 || i == 7) addY = 1;

                    if (i % 2 == 0) { forbiddenPieces[0] = 1; forbiddenPieces[1] = 4; }
                    else { forbiddenPieces[0] = 3; forbiddenPieces[1] = 4; }

                    while ((currX > 0 || addX != -1) && (currX < 7 || addX != 1) && (currY > 0 || addY != -1) && (currY < 7 || addY != 1) && flag)
                    {
                        currX += addX;
                        currY += addY;

                        // se la casella controllata non è vuota
                        if (_pieces[currY, currX] != 0)
                        {
                            // se è nero
                            if (_pieces[currY, currX] > 20 && _pieces[currY, currX] < 30 && (_pieces[currY, currX] % 10 == forbiddenPieces[0] || _pieces[currY, currX] % 10 == forbiddenPieces[1]))
                                return false;
                            else
                                flag = false;
                        }
                    }
                }
            }

            _check = 0;
            // se muovo il re arrocco impossibile da rifare
            _arroccoC = -1;
            _arroccoL = -1;

            return true;
        }

        private bool TryToRemoveCheck(int oldX, int oldY, int newX, int newY)
        {
            int temp = _pieces[oldY, oldX];
            int temp2 = _pieces[newY, newX];
            _pieces[oldY, oldX] = 0;
            _pieces[newY, newX] = temp;

            bool[] direz = { true, true, true, true, true, true, true, true };

            int addX = 0;
            int addY = 0;
            int currX;
            int currY;
            bool flag;
            int[] forbiddenPieces = new int[2];

            // in caso di pendoni che fanno scacco
            if (_yRe > 0)
            {
                if (_xRe > 0 && _pieces[_yRe - 1, _xRe - 1] == 26) return true;
                if (_xRe < 7 && _pieces[_yRe - 1, _xRe + 1] == 26) return true;
            }


            // in caso di cavalli che fanno scacco
            if (_yRe - 1 >= 0 && _xRe + 2 <= 7 && _pieces[_yRe - 1, _xRe + 2] == 22) return true;
            if (_yRe - 2 >= 0 && _xRe + 1 <= 7 && _pieces[_yRe - 2, _xRe + 1] == 22) return true;
            if (_yRe - 2 >= 0 && _xRe - 1 >= 0 && _pieces[_yRe - 2, _xRe - 1] == 22) return true;
            if (_yRe - 1 >= 0 && _xRe - 2 >= 0 && _pieces[_yRe - 1, _xRe - 2] == 22) return true;
            if (_yRe + 1 <= 7 && _xRe - 2 >= 0 && _pieces[_yRe + 1, _xRe - 2] == 22) return true;
            if (_yRe + 2 <= 7 && _xRe - 1 >= 0 && _pieces[_yRe + 2, _xRe - 1] == 22) return true;
            if (_yRe + 2 <= 7 && _xRe + 1 <= 7 && _pieces[_yRe + 2, _xRe + 1] == 22) return true;
            if (_yRe + 1 <= 7 && _xRe + 2 <= 7 && _pieces[_yRe + 1, _xRe + 2] == 22) return true;


            for (int i = 0; i < 8; i++)
            {
                flag = true;
                addX = 0;
                addY = 0;
                currX = _xRe;
                currY = _yRe;

                // in base alla direzione si determina il valore da sommare alle coordinate ciascun controllo
                if (i == 7 || i == 0 || i == 1) addX = 1;
                if (i == 1 || i == 2 || i == 3) addY = -1;
                if (i == 3 || i == 4 || i == 5) addX = -1;
                if (i == 5 || i == 6 || i == 7) addY = 1;

                if (i % 2 == 0) { forbiddenPieces[0] = 1; forbiddenPieces[1] = 4; }
                else { forbiddenPieces[0] = 3; forbiddenPieces[1] = 4; }

                while ((currX > 0 || addX != -1) && (currX < 7 || addX != 1) && (currY > 0 || addY != -1) && (currY < 7 || addY != 1) && flag)
                {
                    currX += addX;
                    currY += addY;

                    // se la casella controllata non è vuota
                    if (_pieces[currY, currX] != 0)
                    {
                        // se è nero
                        if (_pieces[currY, currX] > 20 && _pieces[currY, currX] < 30)
                        {

                            if (_pieces[currY, currX] % 10 == forbiddenPieces[0] || _pieces[currY, currX] % 10 == forbiddenPieces[1])
                            {
                                _pieces[oldY, oldX] = temp;
                                _pieces[newY, newX] = temp2;

                                return true;
                            }
                        }
                        else
                            flag = false;
                    }
                }
            }

            _pieces[oldY, oldX] = temp;
            _pieces[newY, newX] = temp2;

            _check = 0;
            return false;
        }

        private bool[] UpdateChessboard(int x1, int y1, int x2, int y2)
        {
            int temp = _pieces[y1, x1];
            _pieces[y1, x1] = 0;
            _pieces[y2, x2] = temp;

            bool[] direz = { false, false, false, false, false, false, false, false };
            int[,] coord = {
                 { x1, y1},
                 { x2, y2}
            };

            for (int i = 0; i < 2; i++)
            {
                if (_yRe == coord[i, 1])
                {
                    if (_xRe < coord[i, 0]) direz[0] = true;
                    else direz[4] = true;
                }
                if (_xRe == coord[i, 0])
                {
                    if (_yRe < coord[i, 1]) direz[6] = true;
                    else direz[2] = true;
                }
                if (_yRe - _xRe == coord[i, 1] - coord[i, 0])
                {
                    if (_xRe < coord[i, 0]) direz[7] = true;
                    else direz[3] = true;
                }
                if (_yRe + _xRe == coord[i, 1] + coord[i, 0])
                {
                    if (_xRe < coord[i, 0]) direz[1] = true;
                    else direz[5] = true;
                }
            }

            return direz;
        }
        // aggiorna l'array di numeri interi contenente lo stato della scahcchiera

        private void UpdateStateOfPieces(bool[] direz)
        {
            int addX = 0;
            int addY = 0;
            int memX = 0;
            int memY = 0;
            int currX;
            int currY;
            int flag;
            int[] forbiddenPieces = new int[2];

            // in caso di pendoni che fanno scacco
            if (_yRe > 0)
            {
                if (_xRe > 0 && _pieces[_yRe - 1, _xRe - 1] == 26) { _checkPieces[_check, 0] = _yRe - 1; _checkPieces[_check, 1] = _xRe - 1; _check++; };
                if (_xRe < 7 && _pieces[_yRe - 1, _xRe + 1] == 26) { _checkPieces[_check, 0] = _yRe - 1; _checkPieces[_check, 1] = _xRe + 1; _check++; };
            }


            // in caso di cavalli che fanno scacco
            if (_yRe - 1 >= 0 && _xRe + 2 <= 7 && _pieces[_yRe - 1, _xRe + 2] == 22) { _checkPieces[_check, 0] = _yRe - 1; _checkPieces[_check, 1] = _xRe + 2; _check++; };
            if (_yRe - 2 >= 0 && _xRe + 1 <= 7 && _pieces[_yRe - 2, _xRe + 1] == 22) { _checkPieces[_check, 0] = _yRe - 2; _checkPieces[_check, 1] = _xRe + 1; _check++; };
            if (_yRe - 2 >= 0 && _xRe - 1 >= 0 && _pieces[_yRe - 2, _xRe - 1] == 22) { _checkPieces[_check, 0] = _yRe - 2; _checkPieces[_check, 1] = _xRe - 1; _check++; };
            if (_yRe - 1 >= 0 && _xRe - 2 >= 0 && _pieces[_yRe - 1, _xRe - 2] == 22) { _checkPieces[_check, 0] = _yRe - 1; _checkPieces[_check, 1] = _xRe - 2; _check++; };
            if (_yRe + 1 <= 7 && _xRe - 2 >= 0 && _pieces[_yRe + 1, _xRe - 2] == 22) { _checkPieces[_check, 0] = _yRe + 1; _checkPieces[_check, 1] = _xRe - 2; _check++; };
            if (_yRe + 2 <= 7 && _xRe - 1 >= 0 && _pieces[_yRe + 2, _xRe - 1] == 22) { _checkPieces[_check, 0] = _yRe + 2; _checkPieces[_check, 1] = _xRe - 1; _check++; };
            if (_yRe + 2 <= 7 && _xRe + 1 <= 7 && _pieces[_yRe + 2, _xRe + 1] == 22) { _checkPieces[_check, 0] = _yRe + 2; _checkPieces[_check, 1] = _xRe + 1; _check++; };
            if (_yRe + 1 <= 7 && _xRe + 2 <= 7 && _pieces[_yRe + 1, _xRe + 2] == 22) { _checkPieces[_check, 0] = _yRe + 1; _checkPieces[_check, 1] = _xRe + 2; _check++; };

            for (int i = 0; i < 8; i++)
            {
                // se la direzione è quella giusta
                if (direz[i])
                {
                    addX = 0;
                    addY = 0;
                    currX = _xRe;
                    currY = _yRe;
                    flag = 0;

                    // in base alla direzione si determina il valore da sommare alle coordinate ciascun controllo
                    if (i == 7 || i == 0 || i == 1) addX = 1;
                    if (i == 1 || i == 2 || i == 3) addY = -1;
                    if (i == 3 || i == 4 || i == 5) addX = -1;
                    if (i == 5 || i == 6 || i == 7) addY = 1;

                    if (i % 2 == 0) { forbiddenPieces[0] = 1; forbiddenPieces[1] = 4; }
                    else { forbiddenPieces[0] = 3; forbiddenPieces[1] = 4; }

                    while ((currX > 0 || addX != -1) && (currX < 7 || addX != 1) && (currY > 0 || addY != -1) && (currY < 7 || addY != 1) && flag != 2)
                    {
                        currX += addX;
                        currY += addY;

                        // se la casella controllata non è vuota
                        if (_pieces[currY, currX] != 0)
                        {
                            // se è nero
                            if (_pieces[currY, currX] > 20 && _pieces[currY, currX] < 30)
                            {
                                switch (flag)
                                {
                                    // se non è ancora stato trovato nulla
                                    case 0:
                                        if (_pieces[currY, currX] % 10 == forbiddenPieces[0] || _pieces[currY, currX] % 10 == forbiddenPieces[1])
                                        {
                                            _checkPieces[_check, 0] = currY;
                                            _checkPieces[_check, 1] = currX;
                                            _check++;
                                        }
                                        flag = 2;
                                        break;
                                    // se il pezzo trovato prima era movibile
                                    case 1:
                                        if (_pieces[currY, currX] % 10 == forbiddenPieces[0] || _pieces[currY, currX] % 10 == forbiddenPieces[1])
                                            _pieces[memY, memX] += 20;
                                        flag = 2;
                                        break;
                                }
                            }
                            else
                            {
                                switch (flag)
                                {
                                    case 0:
                                        memX = currX;
                                        memY = currY;
                                        if (_pieces[currY, currX] > 30) _pieces[currY, currX] -= 20;
                                        flag = 1;
                                        break;
                                    case 1:
                                        if (_pieces[currY, currX] > 30)
                                            _pieces[currY, currX] -= 20;
                                        flag = 2;
                                        break;
                                }
                            }
                        }
                    }
                }
            }
        }
        // aggiorna le condizioni delle pedine, nel momento dopo la mossa
        // ovvero le pedine che magari prima non si potevano spostare poichè creavano scacco
        // magari dopo una determinata mossa potranno muoversi

        private void ReleasePieces(bool[] direz, int x, int y)
        {
            int addX = 0;
            int addY = 0;
            int currX;
            int currY;
            bool flag;

            for (int i = 0; i < 8; i++)
            {
                // se la direzione è quella giusta
                if (direz[i])
                {
                    addX = 0;
                    addY = 0;
                    currX = x;
                    currY = y;
                    flag = true;

                    // in base alla direzione si determina il valore da sommare alle coordinate ciascun controllo
                    if (i == 7 || i == 0 || i == 1) addX = 1;
                    if (i == 1 || i == 2 || i == 3) addY = -1;
                    if (i == 3 || i == 4 || i == 5) addX = -1;
                    if (i == 5 || i == 6 || i == 7) addY = 1;

                    while ((currX > 0 || addX != -1) && (currX < 7 || addX != 1) && (currY > 0 || addY != -1) && (currY < 7 || addY != 1) && flag)
                    {

                        currX += addX;
                        currY += addY;

                        // se la casella controllata non è vuota
                        if (_pieces[currY, currX] != 0)
                        {
                            // se è bianco
                            if (_pieces[currY, currX] > 30)
                                _pieces[currY, currX] -= 20;
                            flag = false;
                        }
                    }
                }
            }
        }
        // esclusivamente dopo la mossa del re, le pedine soggette al blocco per 
        // evitare lo scacco precedente vengono liberate dal blocco (se possono). 

        public bool MoveOpponent(int x1, int y1, int x2, int y2)
        {
            bool mangiata = false;

            if (_pieces[y2, x2] != 0)
            {
                mangiata = true;
                _notDead.Remove(10 + _pieces[y2, x2] % 10);
            }    

            bool[] direz = UpdateChessboard(x1, y1, x2, y2);
            UpdateStateOfPieces(direz);

            _enPassant = -1;
            // en passant attivato (quando ricevo la mossa da parte di un pedone che si è mosso di 2) 
            if (_pieces[y2, x2] % 10 == 6 && y2 == y1 + 2)
                _enPassant = x2;

            return mangiata;
        }
        // mossa ricevuta dal server da parte dell'avversario

        public string Draw() {

            string result = String.Empty;

            if (_notDead.Count < 2)
            {
                if (_notDead.Count == 0 || _notDead.Contains(13) || _notDead.Contains(23) || _notDead.Contains(12) || _notDead.Contains(22) || _notDead.Contains(16) || _notDead.Contains(26))
                    result = "DRP";
            }

            return result;
        }

        public bool CheckMate()
        {
            bool[] direz = { true, true, true, true, true, true, true, true };
            int xTemp = _xRe;
            int yTemp = _yRe;

            if (_check == 0) return false;
            else {
                if (_xRe == _checkPieces[0, 1] && !(_yRe == _checkPieces[0, 0] - 1 || _yRe == _checkPieces[0, 0] + 1)) { direz[2] = false; direz[6] = false; }
                if (_yRe == _checkPieces[0, 0] && !(_xRe == _checkPieces[0, 1] - 1 || _xRe == _checkPieces[0, 1] + 1)) { direz[0] = false; direz[4] = false; }
                if (_yRe - _xRe == _checkPieces[0, 0] - _checkPieces[0, 1] && !(_yRe == _checkPieces[0, 0] - 1 || _yRe == _checkPieces[0, 0] + 1)) { direz[3] = false; direz[7] = false; }
                if (_yRe + _xRe == _checkPieces[0, 0] + _checkPieces[0, 1] && !(_yRe == _checkPieces[0, 0] - 1 || _yRe == _checkPieces[0, 0] + 1)) { direz[1] = false; direz[5] = false; }

                if(_check == 2)
                {
                    if (_xRe == _checkPieces[1, 1] && !(_yRe == _checkPieces[1, 0] - 1 || _yRe == _checkPieces[1, 0] + 1)) { direz[2] = false; direz[6] = false; }
                    if (_yRe == _checkPieces[1, 0] && !(_xRe == _checkPieces[1, 1] - 1 || _xRe == _checkPieces[1, 1] + 1)) { direz[0] = false; direz[4] = false; }
                    if (_yRe - _xRe == _checkPieces[1, 0] - _checkPieces[1, 1] && !(_yRe == _checkPieces[1, 0] - 1 || _yRe == _checkPieces[1, 0] + 1)) { direz[3] = false; direz[7] = false; }
                    if (_yRe + _xRe == _checkPieces[1, 0] + _checkPieces[1, 1] && !(_yRe == _checkPieces[1, 0] - 1 || _yRe == _checkPieces[1, 0] + 1)) { direz[1] = false; direz[5] = false; }
                }

                for (int i = 0; i < 8; i++)
                {
                    // se la direzione è quella giusta
                    if (direz[i])
                    {
                        // in base alla direzione si determina il valore della casella dove il re proverà a spostarsi
                        if (i == 7 || i == 0 || i == 1) xTemp = _xRe + 1;
                        if (i == 1 || i == 2 || i == 3) yTemp = _yRe - 1;
                        if (i == 3 || i == 4 || i == 5) xTemp = _xRe - 1;
                        if (i == 5 || i == 6 || i == 7) yTemp = _yRe + 1;

                        // se la casella è esistente
                        if (xTemp >= 0 && xTemp <= 7 && yTemp >= 0 && yTemp <= 7)
                            // se la casella è vuota
                            if (_pieces[yTemp, xTemp] == 0)
                                // se la casella è una soluzione
                                if (KingMove(new bool[] { true, true, true, true, true, true, true, true, }, xTemp, yTemp)) { _check = 2; return false; }
                    }
                }

                if (_check == 1)
                {
                    if (TryToRemoveCheckByEating(_checkPieces[0, 1], _checkPieces[0, 0], new bool[] { true, true, true, true, true, true, true, true, }, true)) return false;
                    if (TryToRemoveCheckByInterfering(_checkPieces[0, 1], _checkPieces[0, 0])) return false;
                }

                return true;
            }
        }

        private bool TryToRemoveCheckByEating(int x, int y, bool[] direz, bool eatingMode)
        {
            int addX = 0;
            int addY = 0;
            int currX;
            int currY;
            bool flag;
            int[] forbiddenPieces = new int[2];

            if (eatingMode)
            {
                // in caso di pendoni che possono mangiare il pezzo che fa scacco
                if (y < 7)
                {
                    if (x > 0 && _pieces[y + 1, x - 1] == 16) return true;
                    if (x < 7 && _pieces[y + 1, x + 1] == 16) return true;
                }

                if (_pieces[y, x] % 10 == 6 && _enPassant > 0 && (_pieces[y, x + 1] == 16 || _pieces[y, x - 1] == 16)) return true;
            }
            else {
                if (y < 7)
                {
                    if (x > 0 && _pieces[y + 1, x] == 16) return true;
                }
            }

            // in caso di cavalli che possono mangiare il pezzo che fa scacco
            if (y - 1 >= 0 && x + 2 <= 7 && _pieces[y - 1, x + 2] == 12) return true;
            if (y - 2 >= 0 && x + 1 <= 7 && _pieces[y - 2, x + 1] == 12) return true;
            if (y - 2 >= 0 && x - 1 >= 0 && _pieces[y - 2, x - 1] == 12) return true;
            if (y - 1 >= 0 && x - 2 >= 0 && _pieces[y - 1, x - 2] == 12) return true;
            if (y + 1 <= 7 && x - 2 >= 0 && _pieces[y + 1, x - 2] == 12) return true;
            if (y + 2 <= 7 && x - 1 >= 0 && _pieces[y + 2, x - 1] == 12) return true;
            if (y + 2 <= 7 && x + 1 <= 7 && _pieces[y + 2, x + 1] == 12) return true;
            if (y + 1 <= 7 && x + 2 <= 7 && _pieces[y + 1, x + 2] == 12) return true;

            for (int i = 0; i < 8; i++)
            {
                if (direz[i])
                {
                    addX = 0;
                    addY = 0;
                    currX = x;
                    currY = y;
                    flag = true;

                    // in base alla direzione si determina il valore da sommare alle coordinate ciascun controllo
                    if (i == 7 || i == 0 || i == 1) addX = 1;
                    if (i == 1 || i == 2 || i == 3) addY = -1;
                    if (i == 3 || i == 4 || i == 5) addX = -1;
                    if (i == 5 || i == 6 || i == 7) addY = 1;

                    if (i % 2 == 0) { forbiddenPieces[0] = 1; forbiddenPieces[1] = 4; }
                    else { forbiddenPieces[0] = 3; forbiddenPieces[1] = 4; }

                    while ((currX > 0 || addX != -1) && (currX < 7 || addX != 1) && (currY > 0 || addY != -1) && (currY < 7 || addY != 1) && flag)
                    {
                        currX += addX;
                        currY += addY;

                        // se la casella controllata non è vuota
                        if (_pieces[currY, currX] != 0)
                        {
                            // se è un pezzo appartenente al proprio schieramento allora esso può mangiare la pedina che fa scacco
                            if (_pieces[currY, currX] < 20 && (_pieces[currY, currX] % 10 == forbiddenPieces[0] || _pieces[currY, currX] % 10 == forbiddenPieces[1]))
                                return true;
                            else
                                flag = false;
                        }
                    }
                }
            }

            return false;
        }

        private bool TryToRemoveCheckByInterfering(int x, int y)
        {
            bool[] direz = { true, true, true, true, true, true, true, true };
            int addX = 0;
            int addY = 0;
            int currX = _xRe;
            int currY = _yRe;

            if (_xRe == x) { direz[2] = false; direz[6] = false;
                if (_yRe < y) addY++;
                else addY--;
            }
            if (_yRe == y) { direz[0] = false; direz[4] = false;
                if (_xRe < x) addX++;
                else addX--;
            }
            if (_yRe - _xRe == y - x) { direz[3] = false; direz[7] = false;
                if (_xRe < x) { addX++; addY++; }
                else { addX--; addY--; }
            }
            if (_yRe + _xRe == y + x) { direz[1] = false; direz[5] = false;
                if (_xRe < x) { addX++; addY--; }
                else { addX--; addY++; }
            }

            currX += addX;
            currY += addY;

            do
            {
                if (TryToRemoveCheckByEating(currX, currY, direz, false)) return true;
                currX += addX;
                currY += addY;
            } while (currX == _xRe && currY == _yRe);

            return false;
        }

        public void MakePromotion(int codePiece, int xPromotion, out string coordSpawn) {
            // 1 = torre
            // 2 = cavallo
            // 3 = alfiere
            // 4 = regina
            int xAppo = 0, yAppo = 0;
            while (_pieces[yAppo, xAppo] != 0)
            {
                xAppo++;
                if (xAppo > 7) { xAppo = 0; yAppo++; }
            }

            _pieces[yAppo, xAppo] = 10 + codePiece;
            coordSpawn = (7 - xAppo).ToString() + (7 - yAppo).ToString();

            bool[] direz = UpdateChessboard(xAppo, yAppo, xPromotion, 0);
            UpdateStateOfPieces(direz);

            _notDead.Remove(16);
            _notDead.Add(10 + codePiece);
        }

        public void UpgradePiece(int x, int y, int pz)
        {
            _pieces[y, x] = pz;
            _notDead.Remove(26);
            _notDead.Add(pz);
        }
    }
}
using System;

class Program
{
    static void Main(string[] args)
    {
        const int SIZE = 15;
        char[,] gameField = new char[SIZE, SIZE];
        Player[] players = new Player[4]
        {
            new Player(0, 0, '$', ConsoleColor.Green),    // Player 1 (top-left)
            new Player(SIZE-1, 0, '!', ConsoleColor.Red), // Player 2 (top-right)
            new Player(0, SIZE-1, '@', ConsoleColor.Blue), // Player 3 (bottom-left)
            new Player(SIZE-1, SIZE-1, '#', ConsoleColor.Yellow) // Player 4 (bottom-right)
        };
        Bomb[] bombs = new Bomb[12];
        int bombCount = 0;
        Random rand = new Random();

        int obstacleCount = 0;
        for (int i = 0; i < SIZE; i++)
        {
            for (int j = 0; j < SIZE; j++)
            {
                if ((i == 0 && j == 0) || (i == SIZE - 1 && j == 0) ||
                    (i == 0 && j == SIZE - 1) || (i == SIZE - 1 && j == SIZE - 1))
                    gameField[i, j] = '*';
                else
                {
                    gameField[i, j] = rand.Next(10) < 3 ? '#' : '*';
                    if (gameField[i, j] == '#') obstacleCount++;
                }
            }
        }

        bool running = true;
        int powerUpCounter = 0;
        bool powerUpActive = false;

        while (running)
        {
            // Place players if alive
            foreach (var player in players)
                if (player.Alive)
                    gameField[player.Y, player.X] = player.Symbol;

            // Place bombs
            for (int b = 0; b < bombCount; b++)
                if (bombs[b].Timer > 0)
                    gameField[bombs[b].Y, bombs[b].X] = 'B';

            // Spawn power-up
            if (!powerUpActive && rand.Next(20) == 0 && powerUpCounter > 15)
            {
                int px, py;
                do
                {
                    px = rand.Next(SIZE);
                    py = rand.Next(SIZE);
                } while (gameField[py, px] != '*');
                gameField[py, px] = 'P';
                powerUpActive = true;
                powerUpCounter = 0;
            }
            powerUpCounter++;

            // Draw game field
            Console.Clear();
            for (int i = 0; i < SIZE; i++)
            {
                for (int j = 0; j < SIZE; j++)
                {
                    bool colored = false;
                    foreach (var player in players)
                    {
                        if (gameField[i, j] == player.Symbol)
                        {
                            Console.ForegroundColor = player.Color;
                            colored = true;
                            break;
                        }
                    }
                    if (!colored)
                    {
                        switch (gameField[i, j])
                        {
                            case '#': Console.ForegroundColor = ConsoleColor.Cyan; break;
                            case '*': Console.ForegroundColor = ConsoleColor.DarkYellow; break;
                            case 'B': Console.ForegroundColor = ConsoleColor.DarkGray; break;
                            case 'P': Console.ForegroundColor = ConsoleColor.Magenta; break;
                        }
                    }
                    Console.Write(gameField[i, j] + " ");
                    Console.ResetColor();
                }
                Console.WriteLine();
            }

            // Display status
            Console.WriteLine("\nPlayer 1 (Green $): W,A,S,D to move, B to bomb, Q to quit");
            Console.WriteLine("Obstacles remaining: " + obstacleCount);
            for (int i = 0; i < players.Length; i++)
                Console.WriteLine($"Player {i + 1} ({players[i].Symbol}) Score: {players[i].Score} | Bombs: {players[i].MaxBombs - players[i].BombCount}/{players[i].MaxBombs} | Alive: {players[i].Alive}");
            for (int b = 0; b < bombCount; b++)
                if (bombs[b].Timer > 0)
                    Console.WriteLine($"Bomb at ({bombs[b].X},{bombs[b].Y}) explodes in {bombs[b].Timer} moves");

            // Player 1 input
            char input = players[0].Alive ? Console.ReadKey(true).KeyChar : ' ';
            int newX = players[0].X;
            int newY = players[0].Y;

            if (players[0].Alive)
            {
                switch (char.ToLower(input))
                {
                    case 'w': if (players[0].Y > 0) newY--; break;
                    case 's': if (players[0].Y < SIZE - 1) newY++; break;
                    case 'a': if (players[0].X > 0) newX--; break;
                    case 'd': if (players[0].X < SIZE - 1) newX++; break;
                    case 'b':
                        if (players[0].BombCount < players[0].MaxBombs && bombCount < 12)
                        {
                            bombs[bombCount] = new Bomb(newX, newY, 5);
                            players[0].BombCount++;
                            bombCount++;
                        }
                        break;
                    case 'q': running = false; break;
                }
            }

            // Move Player 1
            if (running && players[0].Alive && IsValidMove(gameField, newX, newY, players))
            {
                if (gameField[newY, newX] == 'P')
                {
                    players[0].MaxBombs = 3;
                    players[0].Score += 50;
                    powerUpActive = false;
                }
                gameField[players[0].Y, players[0].X] = '*';
                players[0].X = newX;
                players[0].Y = newY;
            }

            // AI Players
            for (int i = 1; i < 4; i++)
            {
                if (!players[i].Alive) continue;
                newX = players[i].X;
                newY = players[i].Y;

                // Check if near a bomb and run away
                bool nearBomb = false;
                for (int b = 0; b < bombCount; b++)
                {
                    if (Math.Abs(bombs[b].X - newX) <= 1 && Math.Abs(bombs[b].Y - newY) <= 1)
                    {
                        nearBomb = true;
                        break;
                    }
                }

                if (nearBomb)
                {
                    // Try to move away from nearest bomb
                    int dx = 0, dy = 0;
                    for (int b = 0; b < bombCount; b++)
                    {
                        if (Math.Abs(bombs[b].X - newX) <= 1 && Math.Abs(bombs[b].Y - newY) <= 1)
                        {
                            dx += newX - bombs[b].X;
                            dy += newY - bombs[b].Y;
                        }
                    }
                    if (dx > 0 && newX < SIZE - 1) newX++;
                    else if (dx < 0 && newX > 0) newX--;
                    if (dy > 0 && newY < SIZE - 1) newY++;
                    else if (dy < 0 && newY > 0) newY--;
                }
                else
                {
                    // Normal movement or bomb placement
                    int action = rand.Next(5);
                    if (action < 4)
                    {
                        switch (action)
                        {
                            case 0: if (players[i].Y > 0) newY--; break;
                            case 1: if (players[i].Y < SIZE - 1) newY++; break;
                            case 2: if (players[i].X > 0) newX--; break;
                            case 3: if (players[i].X < SIZE - 1) newX++; break;
                        }
                    }
                    else if (players[i].BombCount < players[i].MaxBombs && bombCount < 12 &&
                             IsNearObstacle(gameField, newX, newY, SIZE))
                    {
                        bombs[bombCount] = new Bomb(newX, newY, 5);
                        players[i].BombCount++;
                        bombCount++;
                        // Immediately try to move away
                        int[] directions = { 0, 1, 2, 3 };
                        Shuffle(directions, rand);
                        foreach (int dir in directions)
                        {
                            int tx = newX, ty = newY;
                            if (dir == 0 && ty > 0) ty--;
                            else if (dir == 1 && ty < SIZE - 1) ty++;
                            else if (dir == 2 && tx > 0) tx--;
                            else if (dir == 3 && tx < SIZE - 1) tx++;
                            if (IsValidMove(gameField, tx, ty, players))
                            {
                                newX = tx;
                                newY = ty;
                                break;
                            }
                        }
                    }
                }

                if (IsValidMove(gameField, newX, newY, players))
                {
                    if (gameField[newY, newX] == 'P')
                    {
                        players[i].MaxBombs = 3;
                        players[i].Score += 50;
                        powerUpActive = false;
                    }
                    gameField[players[i].Y, players[i].X] = '*';
                    players[i].X = newX;
                    players[i].Y = newY;
                }
            }

            // Handle bombs
            for (int b = 0; b < bombCount; b++)
            {
                if (bombs[b].Timer > 0)
                {
                    bombs[b].Timer--;
                    if (bombs[b].Timer == 0)
                    {
                        int destroyed = ExplodeBomb(gameField, bombs[b].X, bombs[b].Y, ref obstacleCount, players);
                        int nearestPlayer = FindNearestPlayer(bombs[b].X, bombs[b].Y, players);
                        if (nearestPlayer != -1)
                        {
                            players[nearestPlayer].Score += destroyed * 10;
                            players[nearestPlayer].BombCount--;
                        }

                        for (int j = b; j < bombCount - 1; j++)
                            bombs[j] = bombs[j + 1];
                        bombCount--;
                        b--;
                    }
                }
            }

            // Check game state
            int aliveCount = players.Count(p => p.Alive);
            if (aliveCount <= 1 || (!running && aliveCount > 0) || obstacleCount <= 0)
            {
                running = false;
                DisplayEndScreen(players);
            }
        }
    }

    class Player
    {
        public int X, Y, Score, BombCount, MaxBombs;
        public char Symbol;
        public ConsoleColor Color;
        public bool Alive;
        public Player(int x, int y, char symbol, ConsoleColor color)
        {
            X = x; Y = y; Score = 0; BombCount = 0; MaxBombs = 2; Symbol = symbol; Color = color; Alive = true;
        }
    }

    class Bomb
    {
        public int X, Y, Timer;
        public Bomb(int x, int y, int timer) { X = x; Y = y; Timer = timer; }
    }

    static bool IsValidMove(char[,] field, int x, int y, Player[] players)
    {
        return field[y, x] != '#' && field[y, x] != 'B' &&
               !Array.Exists(players, p => p.Alive && p.X == x && p.Y == y);
    }

    static bool IsNearObstacle(char[,] field, int x, int y, int size)
    {
        return (y > 0 && field[y - 1, x] == '#') || (y < size - 1 && field[y + 1, x] == '#') ||
               (x > 0 && field[y, x - 1] == '#') || (x < size - 1 && field[y, x + 1] == '#');
    }

    static int ExplodeBomb(char[,] field, int x, int y, ref int obstacleCount, Player[] players)
    {
        int destroyed = 0;
        field[y, x] = '*';

        foreach (var player in players)
        {
            if (!player.Alive) continue;
            if ((player.X == x && player.Y == y) ||
                (player.X == x && player.Y == y - 1) || (player.X == x && player.Y == y + 1) ||
                (player.X == x - 1 && player.Y == y) || (player.X == x + 1 && player.Y == y))
            {
                player.Alive = false;
                field[player.Y, player.X] = '*';
            }
        }

        if (y > 0 && field[y - 1, x] == '#') { field[y - 1, x] = '*'; destroyed++; obstacleCount--; }
        if (y < 14 && field[y + 1, x] == '#') { field[y + 1, x] = '*'; destroyed++; obstacleCount--; }
        if (x > 0 && field[y, x - 1] == '#') { field[y, x - 1] = '*'; destroyed++; obstacleCount--; }
        if (x < 14 && field[y, x + 1] == '#') { field[y, x + 1] = '*'; destroyed++; obstacleCount--; }
        return destroyed;
    }

    static int FindNearestPlayer(int x, int y, Player[] players)
    {
        int nearest = -1;
        double minDist = double.MaxValue;
        for (int i = 0; i < 4; i++)
        {
            if (!players[i].Alive) continue;
            double dist = Math.Sqrt(Math.Pow(players[i].X - x, 2) + Math.Pow(players[i].Y - y, 2));
            if (dist < minDist)
            {
                minDist = dist;
                nearest = i;
            }
        }
        return nearest;
    }

    static void DisplayEndScreen(Player[] players)
    {
        Console.Clear();
        int aliveCount = players.Count(p => p.Alive);
        if (aliveCount == 1)
        {
            int winner = Array.FindIndex(players, p => p.Alive);
            Console.WriteLine($"Player {winner + 1} ({players[winner].Symbol}) Wins by Survival!");
        }
        else if (aliveCount == 0)
            Console.WriteLine("Game Over! All players eliminated!");
        else
            Console.WriteLine("Game Over! Multiple players survived or obstacles cleared!");

        for (int i = 0; i < 4; i++)
            Console.WriteLine($"Player {i + 1} ({players[i].Symbol}) Score: {players[i].Score} | Alive: {players[i].Alive}");

        int maxScore = players.Max(p => p.Score);
        int winnerIdx = Array.FindIndex(players, p => p.Score == maxScore);
        Console.WriteLine($"\nHighest Score: Player {winnerIdx + 1} ({players[winnerIdx].Symbol}) with {maxScore} points!");
    }

    static void Shuffle(int[] array, Random rand)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = rand.Next(i + 1);
            int temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
    }
}
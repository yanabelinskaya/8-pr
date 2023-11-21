using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

class User
{
    public string Name { get; set; }
    public int CharactersPerMinute { get; set; }
    public int CharactersPerSecond { get; set; }
}

static class Leaderboard
{
    private static List<User> leaderboard = new List<User>();

    public static void AddToLeaderboard(User user)
    {
        leaderboard.Add(user);
        leaderboard = leaderboard.OrderByDescending(u => u.CharactersPerMinute).ToList();
        SaveLeaderboard();
    }

    public static void DisplayLeaderboard()
    {
        Console.WriteLine("Таблица рекордов:");
        foreach (var user in leaderboard)
        {
            Console.WriteLine($"{user.Name} - {user.CharactersPerMinute} CPM, {user.CharactersPerSecond} CPS");
        }
    }

    public static void ResetLeaderboard()
    {
        leaderboard.Clear();
        SaveLeaderboard();
    }

    private static void SaveLeaderboard()
    {
        try
        {
            string json = JsonConvert.SerializeObject(leaderboard);
            File.WriteAllText("leaderboard.json", json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при сохранении таблицы рекордов: {ex.Message}");
        }
    }
}

class TypingTest
{
    private static string sampleText = "Компьютерная сеть - группа компьютеров, соединенных друг с другом каналами связи. Сети подразделяются по территориальному признаку: локальные сети и глобальные сети. Локальная сеть расположена в одном или некоторых близлежащих зданиях. Обычно локальные сети используют в рамках одной компании. Из-за этого их называют корпоративными. Глобальные сети - совокупность локальных сетей, соединенных канальными связями. Подразделяются на городскую, региональную, национальную и транснациональную. Преимущества сети: совместное использование информации, совместное использование аппаратных средств, совместное использование программных средств, обмен сообщениями. Недостатки: быстрое распространение вирусов и возможность взлома.";
    private static bool isTyping = true;
    private static string input = "";
    private static int remainingTime = 60; // Время в секундах
    private static object lockObject = new object(); // Объект блокировки

    private static void HighlightInput(string input, string sampleText, int remainingTime)
    {
        Console.Clear();
        Console.WriteLine("Введите следующий текст:\n");

        lock (lockObject)
        {
            for (int i = 0; i < sampleText.Length; i++)
            {
                if (i < input.Length)
                {
                    char inputChar = input[i];
                    char sampleChar = sampleText[i];

                    Console.ForegroundColor = (char.ToLower(input[i]) == char.ToLower(sampleText[i])) ? ConsoleColor.Green : ConsoleColor.Red;
                    if (char.IsLetterOrDigit(sampleChar) || char.IsWhiteSpace(sampleChar))
                    {
                        Console.Write(inputChar);
                    }
                    else
                    {
                        Console.Write(sampleChar);
                        i++;
                    }
                    Console.ResetColor();
                }
                else
                {
                    Console.Write(sampleText[i]); // Выводим оставшуюся часть текста без подсветки
                }
            }
        }

        Console.SetCursorPosition(0, 10);
        Console.WriteLine($"Оставшееся время: {remainingTime} сек");
    }

    private static void CalculateStats(string username, Stopwatch stopwatch)
    {
        int charactersTyped = input.Count(char.IsLetterOrDigit);
        int secondsPassed = (int)stopwatch.Elapsed.TotalSeconds;

        User user = new User
        {
            Name = username,
            CharactersPerMinute = (int)((charactersTyped / 5.0) / (stopwatch.Elapsed.TotalMinutes)),
            CharactersPerSecond = charactersTyped / secondsPassed
        };

        Leaderboard.AddToLeaderboard(user);
        Leaderboard.DisplayLeaderboard();

        Console.WriteLine($"Результаты теста: {charactersTyped} символов за {secondsPassed} секунд. {user.CharactersPerMinute} CPM, {user.CharactersPerSecond} CPS");
    }


    private static void Timer()
    {
        while (remainingTime > 0)
        {
            lock (lockObject)
            {
                if (isTyping)
                {
                    Console.SetCursorPosition(0, 10);
                    Console.WriteLine($"Оставшееся время: {remainingTime} сек");
                }
            }

            Thread.Sleep(1000);
            remainingTime--;

            if (!isTyping)
                break;
        }
    }

    public static void StartTest(string username)
    {
        Console.Clear();
        Console.WriteLine("Введите следующий текст:\n");
        Console.WriteLine(sampleText);
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.WriteLine();

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        Thread timerThread = new Thread(Timer);
        timerThread.Start();

        while (isTyping)
        {
            ConsoleKeyInfo key = Console.ReadKey(true);

            lock (lockObject)
            {
                if (remainingTime <= 0)
                {
                    isTyping = false;
                    stopwatch.Stop();
                    CalculateStats(username, stopwatch);
                }
                else if (key.Key == ConsoleKey.Backspace && input.Length > 0)
                {
                    input = input.Substring(0, input.Length - 1);
                }
                else if (key.Key != ConsoleKey.Enter)
                {
                    input += key.KeyChar;
                }

                HighlightInput(input, sampleText, remainingTime);

                if (input == sampleText || !isTyping)
                {
                    isTyping = false;
                    stopwatch.Stop();
                    CalculateStats(username, stopwatch);
                }
            }
        }

        timerThread.Join(); // Дождитесь завершения потока таймера
        Leaderboard.DisplayLeaderboard();


        Console.WriteLine("Хотите пройти тест еще раз? (да/нет): ");
        string repeatChoice = Console.ReadLine().ToLower();

        if (repeatChoice == "да")
        {
            Leaderboard.ResetLeaderboard();
            isTyping = true;
            remainingTime = 60;
            input = "";
            StartTest(username);
        }
        else if (repeatChoice == "нет")
        {
            Environment.Exit(0);
        }
    }
}

class Program
{
    static void Main()
    {
        bool repeatTest = true;

        while (repeatTest)
        {
            Console.Write("Введите ваше имя: ");
            string username = Console.ReadLine();

            TypingTest.StartTest(username);

            Console.Write("Хотите пройти тест еще раз? (y/n): ");
            char repeatChoice = Console.ReadKey().KeyChar;

            repeatTest = (repeatChoice == 'y' || repeatChoice == 'Y');
            Console.WriteLine();
        }

        Console.ReadLine();
    }
}


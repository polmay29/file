using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

class Program
{
    private static System.Timers.Timer timer; // Таймер для автоматического обновления файла
    private static Mutex mutex = new Mutex(); // Для синхронизации потоков

    static async Task Main()
    {
        // Запускаем таймер, который каждые 5 секунд обновляет файл
        timer = new System.Timers.Timer(5000);
        timer.Elapsed += UpdateFile; // Обновление файла по таймеру
        timer.Start();

        // Запускаем поток для ввода данных от пользователя
        Thread inputThread = new Thread(HandleUserInput);
        inputThread.Start();

        // Выполняем многопоточную обработку данных из файла
        ProcessFileData();

        // Выполняем HTTP-запрос к серверу
        await GetDataFromServer();
    }

    // Функция для ввода данных от пользователя и записи их в файл
    static void HandleUserInput()
    {
        while (true)
        {
            Console.WriteLine("Введите строку для добавления в файл (или 'exit' для завершения):");
            string input = Console.ReadLine();

            if (input.ToLower() == "exit")
                break;

            // Запись данных в файл
            using (StreamWriter sw = File.AppendText("lod.txt"))
            {
                sw.WriteLine(input);
            }

            Console.WriteLine($"Данные '{input}' добавлены в файл.");
        }
    }

    // Функция для автоматического обновления файла каждые 5 секунд
    private static void UpdateFile(Object source, ElapsedEventArgs e)
    {
        mutex.WaitOne(); // Синхронизируем доступ к файлу

        using (StreamWriter sw = File.AppendText("lod.txt"))
        {
            sw.WriteLine($"Автообновление: {DateTime.Now}"); // Добавляем время в файл
        }

        Console.WriteLine("Файл автоматически обновлен.");
        mutex.ReleaseMutex(); // Разблокируем доступ для других потоков
    }

    // Многопоточная обработка данных: вычисление среднего и максимума
    static void ProcessFileData()
    {
        try
        {
            string[] lines = File.ReadAllLines("lod.txt");
            int[] numbers = Array.ConvertAll(lines, line =>
            {
                return int.TryParse(line, out int num) ? num : 0; // Преобразуем строки в числа
            });

            // Запускаем два потока для вычисления среднего и максимума
            Thread avgThread = new Thread(() => CalculateAverage(numbers));
            Thread maxThread = new Thread(() => FindMax(numbers));

            avgThread.Start();
            maxThread.Start();

            avgThread.Join();
            maxThread.Join();
        }
        catch (IOException e)
        {
            Console.WriteLine("Ошибка работы с файлом: " + e.Message);
        }
    }

    // Вычисление среднего значения чисел
    static void CalculateAverage(int[] numbers)
    {
        mutex.WaitOne();
        double average = 0;
        int count = 0;

        foreach (int num in numbers)
        {
            if (num != 0)
            {
                average += num;
                count++;
            }
        }

        average = count > 0 ? average / count : 0;
        Console.WriteLine($"Среднее значение: {average}");
        mutex.ReleaseMutex();
    }

    // Нахождение максимального значения
    static void FindMax(int[] numbers)
    {
        mutex.WaitOne();
        int max = int.MinValue;

        foreach (int num in numbers)
        {
            if (num > max)
                max = num;
        }

        Console.WriteLine($"Максимальное значение: {max}");
        mutex.ReleaseMutex();
    }

    // Функция для получения данных с сервера через HTTP-запрос
    static async Task GetDataFromServer()
    {
        HttpClient client = new HttpClient();
        try
        {
            string result = await client.GetStringAsync("https://jsonplaceholder.typicode.com/posts/1");
            Console.WriteLine("Данные с сервера:");
            Console.WriteLine(result);
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("Ошибка при отправке HTTP-запроса: " + e.Message);
        }
    }
}

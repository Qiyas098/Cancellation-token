using System;
using System.Diagnostics;
using System.IO;
using System.Threading;


class Program
{
    static Semaphore semaphore = new Semaphore(3, 4);
    static CancellationTokenSource cts = new CancellationTokenSource();

    static string folder = Path.Combine(AppContext.BaseDirectory, "Downloads");

    static string[] files =
    {
        "video1.txt",
        "video2.txt",
        "video3.txt",
        "video4.txt"
    };

    static long[] sizes =
    {
        25,
        50,
        100,
        40
    };

    static string[] status = new string[4];
    static int[] progress = new int[4];

    static string[] urls =
    {
        "https://proof.ovh.net/files/10Mb.dat",
        "https://proof.ovh.net/files/10Mb.dat",
        "https://proof.ovh.net/files/10Mb.dat",
        "https://proof.ovh.net/files/10Mb.dat"
    };

    static void Main()
    {
        CreateFiles();

        for (int i = 0; i < 4; i++)
        {
            status[i] = "Waiting";
            progress[i] = 0;
        }

        for (int i = 0; i < 4; i++)
        {
            int index = i;

            ThreadPool.QueueUserWorkItem(x =>
            {
                Download(index);
            });
        }

        while (!cts.IsCancellationRequested)
        {
            Console.Clear();
            ShowScreen();

            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Escape)
                {
                    cts.Cancel();
                    break;
                }
            }

            bool finished = true;

            for (int i = 0; i < 4; i++)
            {
                if (status[i] != "Completed" &&
                    status[i] != "Cancelled")
                {
                    finished = false;
                    break;
                }
            }

            if (finished)
            {
                break;
            }

            Thread.Sleep(100);
        }

        Console.Clear();
        ShowScreen();

        Console.WriteLine();
        Console.WriteLine("Butun yuklemeler bitdi.");
        Console.WriteLine("Proqrami baglamaq ucun ENTER duymesine basin.");

        Console.ReadLine();
    }

    static void CreateFiles()
    {
        Directory.CreateDirectory(folder);

        for (int i = 0; i < files.Length; i++)
        {
            string path = Path.Combine(folder, files[i]);

            if (!File.Exists(path))
            {
                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                    fs.SetLength(0);
                }
            }
        }
    }

    static void Download(int index)
    {
        semaphore.WaitOne();

        try
        {
            if (cts.Token.IsCancellationRequested)
            {
                status[index] = "Cancelled";
                return;
            }

            status[index] = "Downloading";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "chrome.exe",
                    Arguments = urls[index],
                    UseShellExecute = true
                });
            }
            catch
            {
            }

            int totalMb = (int)sizes[index];

            for (int mb = 0; mb <= totalMb; mb++)
            {
                if (cts.Token.IsCancellationRequested)
                {
                    status[index] = "Cancelled";
                    return;
                }

                progress[index] = mb * 100 / totalMb;

                Thread.Sleep(100);
            }

            progress[index] = 100;
            status[index] = "Completed";
        }
        finally
        {
            semaphore.Release();
        }
    }

    static void ShowScreen()
    {
        Console.WriteLine("==============================================");
        Console.WriteLine("       MULTI-THREAD VIDEO DOWNLOADER");
        Console.WriteLine("       Semaphore: MAXIMUM 3");
        Console.WriteLine("==============================================");
        Console.WriteLine();
        Console.WriteLine("1-3  -> Downloading");
        Console.WriteLine("4    -> Waiting");
        Console.WriteLine("ESC  -> Cancel ALL");
        Console.WriteLine();

        int active = 0;
        int waiting = 0;
        int completed = 0;
        int cancelled = 0;

        for (int i = 0; i < files.Length; i++)
        {
            Console.Write($"#{i + 1}   ");

            if (status[i] == "Downloading")
            {
                Console.Write("Downloading ");
                active++;
            }
            else if (status[i] == "Waiting")
            {
                Console.Write("Waiting     ");
                waiting++;
            }
            else if (status[i] == "Completed")
            {
                Console.Write("Completed   ");
                completed++;
            }
            else
            {
                Console.Write("Cancelled   ");
                cancelled++;
            }

            Console.Write("[");

            int bars = progress[i] / 5;

            for (int j = 0; j < 20; j++)
            {
                if (j < bars)
                    Console.Write("#");
                else
                    Console.Write(".");
            }

            Console.WriteLine($"] {progress[i]}%");

            Console.WriteLine($"      {files[i]}   {sizes[i]} MB");
            Console.WriteLine();
        }

        Console.WriteLine("----------------------------------------------");

        Console.WriteLine(
            $"Aktiv: {active} | Gözləyən: {waiting} | " +
            $"Tamamlanan: {completed} | Xəta: {cancelled}");

        Console.OutputEncoding = System.Text.Encoding.UTF8;
    }
}

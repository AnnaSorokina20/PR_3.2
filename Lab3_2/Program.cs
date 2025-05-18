using System;
using System.Linq;
using System.Globalization;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        // Зчитуємо розміри
        Console.Write("Введіть кількість стратегій (рядків) m: ");
        int m = int.Parse(Console.ReadLine() ?? "0");
        Console.Write("Введіть кількість станів природи (стовпців) n: ");
        int n = int.Parse(Console.ReadLine() ?? "0");

        // Створюємо та заповнюємо матрицю по рядках
        double[,] U = new double[m, n];
        Console.WriteLine("\nВведіть кожен рядок матриці корисності U з " +
                          $"{n} чисел, розділених пробілом:");
        for (int i = 0; i < m; i++)
        {
            Console.Write($"Рядок {i + 1}: ");
            var parts = (Console.ReadLine() ?? "")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Take(n)
                .ToArray();
            if (parts.Length < n)
                throw new ArgumentException($"Потрібно ввести щонайменше {n} чисел.");
            for (int j = 0; j < n; j++)
                U[i, j] = double.Parse(parts[j], CultureInfo.InvariantCulture);
        }

        Console.WriteLine();
        ApplyWaldCriterion(U);
    }

    static void ApplyWaldCriterion(double[,] U)
    {
        int m = U.GetLength(0), n = U.GetLength(1);

        Console.WriteLine("Згенерований протокол обчислення:\n");

        // Заголовок стовпців
        Console.Write("     ");
        for (int j = 0; j < n; j++)
            Console.Write($"   P{j + 1}");
        Console.WriteLine();

        // Вивід матриці з підписами рядків
        for (int i = 0; i < m; i++)
        {
            Console.Write($"A{i + 1}: ");
            for (int j = 0; j < n; j++)
                Console.Write($"{U[i, j],6:F2}");
            Console.WriteLine();
        }

        // Мінімум у кожному рядку
        double[] rowMins = new double[m];
        for (int i = 0; i < m; i++)
            rowMins[i] = Enumerable.Range(0, n)
                                   .Select(j => U[i, j])
                                   .Min();

        Console.WriteLine("\nКритерій Вальда:");
        for (int i = 0; i < m; i++)
            Console.WriteLine($"min в рядку A{i + 1}: {rowMins[i]:F2}");

        // Максимум серед мінімумів
        double bestVal = rowMins.Max();
        var bestStrats = rowMins
                         .Select((v, i) => (v, i))
                         .Where(x => Math.Abs(x.v - bestVal) < 1e-9)
                         .Select(x => $"A{x.i + 1}")
                         .ToArray();

        Console.WriteLine($"\nМаксимальний серед мінімальних елементів: {bestVal:F2}");
        Console.WriteLine("Оптимальні стратегії: " + string.Join(", ", bestStrats));
    }
}

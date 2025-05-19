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
        Console.WriteLine();
        ApplyMaximaxCriterion(U);
        Console.WriteLine();
        ApplyHurwiczCriterion(U);
        Console.WriteLine();
        ApplySavageCriterion(U);

        //  ЧИТАЄМО ймовірності для критерію Байєса
        int cols = U.GetLength(1);           
        Console.WriteLine($"Введіть {cols} ймовірностей p1…pn через пробіл (сума = 1):");
        double[] p = Console.ReadLine()
            .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Replace(',', '.'))
            .Select(x => double.Parse(x, CultureInfo.InvariantCulture))
            .ToArray();

        Console.WriteLine();
        ApplyBayesCriterion(U, p);

        Console.WriteLine();
        ApplyLaplaceCriterion(U);
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

    static void ApplyMaximaxCriterion(double[,] U)
    {
        int m = U.GetLength(0), n = U.GetLength(1);

        Console.WriteLine("Критерій максимаксу:\n");

        // Максимум у кожному рядку
        double[] rowMaxs = new double[m];
        for (int i = 0; i < m; i++)
            rowMaxs[i] = Enumerable.Range(0, n)
                                   .Select(j => U[i, j])
                                   .Max();

        for (int i = 0; i < m; i++)
            Console.WriteLine($"max в рядку A{i + 1}: {rowMaxs[i]:F2}");

        // Максимальний серед цих максимумів
        double bestVal = rowMaxs.Max();
        var bestStrats = rowMaxs
                         .Select((v, i) => (v, i))
                         .Where(x => Math.Abs(x.v - bestVal) < 1e-9)
                         .Select(x => $"A{x.i + 1}")
                         .ToArray();

        Console.WriteLine($"\nМаксимальний елемент: {bestVal:F2}");
        Console.WriteLine("Оптимальні стратегії: " + string.Join(", ", bestStrats));
    }

    static void ApplyHurwiczCriterion(double[,] U)
    {
        int m = U.GetLength(0), n = U.GetLength(1);

        Console.WriteLine("Критерій Гурвіца:\n");
        Console.Write("Введіть коефіцієнт y (0…1): ");
        double y = double.Parse(Console.ReadLine() ?? "0", CultureInfo.CurrentCulture);
        Console.WriteLine();

        // Мінімум і максимум у кожному рядку
        double[] rowMins = new double[m];
        double[] rowMaxs = new double[m];
        for (int i = 0; i < m; i++)
        {
            rowMins[i] = Enumerable.Range(0, n).Select(j => U[i, j]).Min();
            rowMaxs[i] = Enumerable.Range(0, n).Select(j => U[i, j]).Max();
            Console.WriteLine($"min в рядку A{i + 1}: {rowMins[i]:F2}");
        }
        Console.WriteLine();
        for (int i = 0; i < m; i++)
            Console.WriteLine($"max в рядку A{i + 1}: {rowMaxs[i]:F2}");

        Console.WriteLine();
        // Обчислюємо зважену оцінку s_i = y*min + (1-y)*max
        double[] s = new double[m];
        for (int i = 0; i < m; i++)
        {
            s[i] = y * rowMins[i] + (1 - y) * rowMaxs[i];
            Console.WriteLine($"s{i + 1} = {y:F1} * {rowMins[i]:F2} + (1 - {y:F1}) * {rowMaxs[i]:F2} = {s[i]:F2}");
        }

        double bestVal = s.Max();
        var bestStrats = s
                         .Select((v, i) => (v, i))
                         .Where(x => Math.Abs(x.v - bestVal) < 1e-9)
                         .Select(x => $"A{x.i + 1}")
                         .ToArray();

        Console.WriteLine($"\nМаксимальний елемент: {bestVal:F2}");
        Console.WriteLine("Оптимальні стратегії: " + string.Join(", ", bestStrats));
    }

    static void ApplySavageCriterion(double[,] U)
    {
        int m = U.GetLength(0), n = U.GetLength(1);
        Console.WriteLine("Критерій Севіджа:\n");

        // Створюємо матрицю ризиків 
        double[,] R = new double[m, n];
        for (int j = 0; j < n; j++)
        {
            // знаходимо max у стовпці j
            double colMax = double.MinValue;
            for (int i = 0; i < m; i++)
                if (U[i, j] > colMax) colMax = U[i, j];

            // заповнюємо ризики
            for (int i = 0; i < m; i++)
                R[i, j] = colMax - U[i, j];
        }

        // Виводимо матрицю ризиків
        Console.WriteLine("Матриця ризиків:");
        for (int i = 0; i < m; i++)
        {
            for (int j = 0; j < n; j++)
                Console.Write($"{R[i, j],6:F2}");
            Console.WriteLine();
        }
        Console.WriteLine();

        //Для кожного рядка знаходимо його максимальний ризик
        double[] rowMaxRisk = new double[m];
        for (int i = 0; i < m; i++)
        {
            rowMaxRisk[i] = Enumerable.Range(0, n)
                                      .Select(j => R[i, j])
                                      .Max();
            Console.WriteLine($"max ризик в рядку A{i + 1}: {rowMaxRisk[i]:F2}");
        }

        // Знаходимо мінімум серед цих максимумів — це критерій мінімаксу
        double bestVal = rowMaxRisk.Min();
        var bestStrats = rowMaxRisk
                         .Select((v, i) => (v, i))
                         .Where(x => Math.Abs(x.v - bestVal) < 1e-9)
                         .Select(x => $"A{x.i + 1}")
                         .ToArray();

        Console.WriteLine($"\nМінімальний серед максимальних ризиків: {bestVal:F2}");
        Console.WriteLine("Оптимальні стратегії: " + string.Join(", ", bestStrats));
    }

    static void ApplyBayesCriterion(double[,] U, double[] p)
    {
        int m = U.GetLength(0), n = U.GetLength(1);
        Console.WriteLine("Критерій Байєса:\n");

        // Перевіримо, що кількість ймовірностей збігається з кількістю стовпців
        if (p.Length != n)
        {
            Console.WriteLine("Кількість ймовірностей не відповідає кількості станів природи.");
            return;
        }

        // Виводимо ймовірності
        Console.WriteLine("Ймовірності станів природи:");
        for (int j = 0; j < n; j++)
            Console.WriteLine($"p{j + 1} = {p[j]:F2}");
        Console.WriteLine();

        // Обчислюємо очікуваний виграш для кожної стратегії
        double[] expected = new double[m];
        for (int i = 0; i < m; i++)
        {
            double sum = 0;
            for (int j = 0; j < n; j++)
                sum += U[i, j] * p[j];
            expected[i] = sum;
            Console.WriteLine($"s{i + 1} = " +
                string.Join(" + ", Enumerable.Range(0, n)
                    .Select(j => $"{U[i, j]}*{p[j]:F2}")) +
                $" = {sum:F2}");
        }

        // Знаходимо максимальне очікуване значення
        double bestVal = expected.Max();
        var bestStrats = expected
            .Select((v, i) => (v, i))
            .Where(x => Math.Abs(x.v - bestVal) < 1e-9)
            .Select(x => $"A{x.i + 1}")
            .ToArray();

        Console.WriteLine($"\nМаксимальний очікуваний виграш: {bestVal:F2}");
        Console.WriteLine("Оптимальні стратегії: " + string.Join(", ", bestStrats));
    }

    static void ApplyLaplaceCriterion(double[,] U)
    {
        int m = U.GetLength(0), n = U.GetLength(1);
        Console.WriteLine("Критерій Лапласа:\n");

        double p = 1.0 / n;
        // Обчислюємо середнє значення для кожної стратегії
        double[] s = new double[m];
        for (int i = 0; i < m; i++)
        {
            // формуємо рядок виду "U[i,0]*p + U[i,1]*p + ... "
            var terms = Enumerable.Range(0, n)
                                  .Select(j => $"{U[i, j]}*{p:F2}");
            double sum = Enumerable.Range(0, n)
                                   .Select(j => U[i, j] * p)
                                   .Sum();
            s[i] = sum;
            Console.WriteLine($"s{i + 1} = "
                + string.Join(" + ", terms)
                + $" = {sum:F2}");
        }

        // Знаходимо максимальне серед цих середніх
        double bestVal = s.Max();
        var bestStrats = s
            .Select((v, i) => (v, i))
            .Where(x => Math.Abs(x.v - bestVal) < 1e-9)
            .Select(x => $"A{x.i + 1}")
            .ToArray();

        Console.WriteLine($"\nМаксимальний елемент: {bestVal:F2}");
        Console.WriteLine("Оптимальні стратегії: " + string.Join(", ", bestStrats));
    }


}

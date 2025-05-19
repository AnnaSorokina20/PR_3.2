using System;
using System.Linq;
using System.Globalization;

class Program
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.InputEncoding = System.Text.Encoding.UTF8;

        int m = ReadInt("Введіть кількість стратегій (рядків) m: ");
        int n = ReadInt("Введіть кількість станів природи (стовпців) n: ");

        double[,] U = ReadMatrix(m, n);

        Console.WriteLine();

        var winners = new List<string>();

        winners.AddRange(ApplyWaldCriterion(U));
        Console.WriteLine();
        winners.AddRange(ApplyMaximaxCriterion(U));
        Console.WriteLine();
        winners.AddRange(ApplyHurwiczCriterion(U));
        Console.WriteLine();
        winners.AddRange(ApplySavageCriterion(U));

        double[] p = ReadProbabilities(n);

        Console.WriteLine();
        winners.AddRange(ApplyBayesCriterion(U, p));
        Console.WriteLine();
        winners.AddRange(ApplyLaplaceCriterion(U));

        var mostCommon = winners
            .GroupBy(s => s)
            .OrderByDescending(g => g.Count())
            .First()
            .Key;

        Console.WriteLine($"\nНайчастіше були оптимальні стратегії: {mostCommon}");
    }

    static int ReadInt(string prompt)
    {
        Console.Write(prompt);
        return int.Parse(Console.ReadLine() ?? "0", CultureInfo.InvariantCulture);
    }




    static double[,] ReadMatrix(int m, int n)
    {
        Console.WriteLine($"\nВведіть кожен рядок матриці корисності U з {n} чисел, розділених пробілом:");
        var U = new double[m, n];
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
                U[i, j] = double.Parse(parts[j].Replace(',', '.'), CultureInfo.InvariantCulture);
        }
        return U;
    }

    static double[] ReadProbabilities(int n)
    {
        Console.WriteLine($"\nВведіть {n} ймовірностей p1…pn через пробіл (сума = 1):");
        var parts = (Console.ReadLine() ?? "")
            .Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Take(n)
            .ToArray();
        if (parts.Length < n)
            throw new ArgumentException($"Потрібно ввести щонайменше {n} ймовірностей.");
        return parts
            .Select(x => double.Parse(x.Replace(',', '.'), CultureInfo.InvariantCulture))
            .ToArray();
    }



    
    static void PrintBest(string message, double bestVal, int[] bestIndices)
    {
        Console.WriteLine($"{message}: {bestVal:F2}");
        Console.WriteLine("Оптимальні стратегії: " + FormatStrategies(bestIndices));
    }

    
    static string FormatStrategies(int[] indices)
    {
        return string.Join(", ", indices.Select(i => $"A{i + 1}"));
    }

    static double[] RowMin(double[,] U)
    {
        int m = U.GetLength(0), n = U.GetLength(1);
        var mins = new double[m];
        for (int i = 0; i < m; i++)
            mins[i] = Enumerable.Range(0, n).Select(j => U[i, j]).Min();
        return mins;
    }

    static double[] RowMax(double[,] U)
    {
        int m = U.GetLength(0), n = U.GetLength(1);
        var maxs = new double[m];
        for (int i = 0; i < m; i++)
            maxs[i] = Enumerable.Range(0, n).Select(j => U[i, j]).Max();
        return maxs;
    }

    static int[] ArgEquals(double[] values, double bestVal, double eps = 1e-9)
    {
        var list = new List<int>();
        for (int i = 0; i < values.Length; i++)
            if (Math.Abs(values[i] - bestVal) < eps)
                list.Add(i);
        return list.ToArray();
    }

    static double[,] ComputeRiskMatrix(double[,] U)
    {
        int m = U.GetLength(0), n = U.GetLength(1);
        var R = new double[m, n];
        for (int j = 0; j < n; j++)
        {
            double colMax = double.MinValue;
            for (int i = 0; i < m; i++)
                if (U[i, j] > colMax) colMax = U[i, j];
            for (int i = 0; i < m; i++)
                R[i, j] = colMax - U[i, j];
        }
        return R;
    }

    static double[] ExpectedValues(double[,] U, double[] p)
    {
        int m = U.GetLength(0), n = U.GetLength(1);
        var exp = new double[m];
        for (int i = 0; i < m; i++)
        {
            double sum = 0;
            for (int j = 0; j < n; j++)
                sum += U[i, j] * p[j];
            exp[i] = sum;
        }
        return exp;
    }


    static double[] LaplaceValues(double[,] U)
    {
        int m = U.GetLength(0), n = U.GetLength(1);
        double p = 1.0 / n;
        var s = new double[m];
        for (int i = 0; i < m; i++)
            s[i] = Enumerable.Range(0, n).Select(j => U[i, j] * p).Sum();
        return s;
    }

    static string[] ApplyWaldCriterion(double[,] U)
    {
        int m = U.GetLength(0);

        Console.WriteLine("Критерій Вальда:\n");

        // мінімум у кожному рядку
        double[] mins = RowMin(U);
        for (int i = 0; i < m; i++)
            Console.WriteLine($"min в рядку A{i + 1}: {mins[i]:F2}");

        // максимальний серед мінімумів
        double bestVal = mins.Max();
        int[] bestIdx = ArgEquals(mins, bestVal);
        string[] bestStrats = bestIdx.Select(i => $"A{i + 1}").ToArray();

        Console.WriteLine($"\nМаксимальний серед мінімальних елементів: {bestVal:F2}");
        Console.WriteLine("Оптимальні стратегії: " + string.Join(", ", bestStrats));

        return bestStrats;
    }

    static string[] ApplyMaximaxCriterion(double[,] U)
    {
        int m = U.GetLength(0);

        Console.WriteLine("Критерій максимаксу:\n");

        // максимум у кожному рядку
        double[] maxs = RowMax(U);
        for (int i = 0; i < m; i++)
            Console.WriteLine($"max в рядку A{i + 1}: {maxs[i]:F2}");

        // максимальний серед максимумів
        double bestVal = maxs.Max();
        int[] bestIdx = ArgEquals(maxs, bestVal);
        string[] bestStrats = bestIdx.Select(i => $"A{i + 1}").ToArray();

        Console.WriteLine($"\nМаксимальний елемент: {bestVal:F2}");
        Console.WriteLine("Оптимальні стратегії: " + string.Join(", ", bestStrats));

        return bestStrats;
    }

    static string[] ApplyHurwiczCriterion(double[,] U)
    {
        int m = U.GetLength(0), n = U.GetLength(1);

        Console.WriteLine("Критерій Гурвіца:\n");
        Console.Write("Введіть коефіцієнт y (0…1): ");
        double y = double.Parse(Console.ReadLine() ?? "0", CultureInfo.CurrentCulture);
        Console.WriteLine();

        // мінімум і максимум у рядках
        double[] mins = RowMin(U), maxs = RowMax(U);
        for (int i = 0; i < m; i++)
            Console.WriteLine($"min в рядку A{i + 1}: {mins[i]:F2}");
        Console.WriteLine();
        for (int i = 0; i < m; i++)
            Console.WriteLine($"max в рядку A{i + 1}: {maxs[i]:F2}");
        Console.WriteLine();

        // обчислюємо si = y*min + (1−y)*max
        double[] s = new double[m];
        for (int i = 0; i < m; i++)
        {
            s[i] = y * mins[i] + (1 - y) * maxs[i];
            Console.WriteLine($"s{i + 1} = {y:F1} * {mins[i]:F2} + (1 - {y:F1}) * {maxs[i]:F2} = {s[i]:F2}");
        }

        double bestVal = s.Max();
        int[] bestIdx = ArgEquals(s, bestVal);
        string[] bestStrats = bestIdx.Select(i => $"A{i + 1}").ToArray();

        Console.WriteLine($"\nМаксимальний елемент: {bestVal:F2}");
        Console.WriteLine("Оптимальні стратегії: " + string.Join(", ", bestStrats));

        return bestStrats;
    }

    static string[] ApplySavageCriterion(double[,] U)
    {
        int m = U.GetLength(0), n = U.GetLength(1);

        Console.WriteLine("Критерій Севіджа:\n");

        // обчислюємо матрицю ризиків
        double[,] R = ComputeRiskMatrix(U);
        Console.WriteLine("Матриця ризиків:");
        for (int i = 0; i < m; i++)
        {
            for (int j = 0; j < n; j++)
                Console.Write($"{R[i, j],6:F2}");
            Console.WriteLine();
        }
        Console.WriteLine();

        // максимум ризику в кожному рядку
        double[] risks = RowMax(R);
        for (int i = 0; i < m; i++)
            Console.WriteLine($"max ризик в рядку A{i + 1}: {risks[i]:F2}");

        // мінімум серед цих максимумів
        double bestVal = risks.Min();
        int[] bestIdx = ArgEquals(risks, bestVal);
        string[] bestStrats = bestIdx.Select(i => $"A{i + 1}").ToArray();

        Console.WriteLine($"\nМінімальний серед максимальних ризиків: {bestVal:F2}");
        Console.WriteLine("Оптимальні стратегії: " + string.Join(", ", bestStrats));

        return bestStrats;
    }

    static string[] ApplyBayesCriterion(double[,] U, double[] p)
    {
        int m = U.GetLength(0), n = U.GetLength(1);

        Console.WriteLine("Критерій Байєса:\n");
        if (p.Length != n)
        {
            Console.WriteLine("Кількість ймовірностей не відповідає кількості станів природи.");
            return Array.Empty<string>();
        }

        Console.WriteLine("Ймовірності станів природи:");
        for (int j = 0; j < n; j++)
            Console.WriteLine($"p{j + 1} = {p[j]:F2}");
        Console.WriteLine();

        // очікувані значення
        double[] exp = ExpectedValues(U, p);
        for (int i = 0; i < m; i++)
        {
            var terms = Enumerable.Range(0, n)
                                  .Select(j => $"{U[i, j]}*{p[j]:F2}");
            Console.WriteLine($"s{i + 1} = {string.Join(" + ", terms)} = {exp[i]:F2}");
        }

        double bestVal = exp.Max();
        int[] bestIdx = ArgEquals(exp, bestVal);
        string[] bestStrats = bestIdx.Select(i => $"A{i + 1}").ToArray();

        Console.WriteLine($"\nМаксимальний очікуваний виграш: {bestVal:F2}");
        Console.WriteLine("Оптимальні стратегії: " + string.Join(", ", bestStrats));

        return bestStrats;
    }


    static string[] ApplyLaplaceCriterion(double[,] U)
    {
        int m = U.GetLength(0), n = U.GetLength(1);

        Console.WriteLine("Критерій Лапласа:\n");

        double[] s = LaplaceValues(U);
        for (int i = 0; i < m; i++)
        {
            var terms = Enumerable.Range(0, n)
                                  .Select(j => $"{U[i, j]}*{1.0 / n:F2}");
            Console.WriteLine($"s{i + 1} = {string.Join(" + ", terms)} = {s[i]:F2}");
        }

        double bestVal = s.Max();
        int[] bestIdx = ArgEquals(s, bestVal);
        string[] bestStrats = bestIdx.Select(i => $"A{i + 1}").ToArray();

        Console.WriteLine($"\nМаксимальний елемент: {bestVal:F2}");
        Console.WriteLine("Оптимальні стратегії: " + string.Join(", ", bestStrats));

        return bestStrats;
    }


}

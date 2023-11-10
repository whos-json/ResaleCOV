using System.Globalization;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

const int LoanTenureYears = 25;

Banner("HDB Resale Cash-Over-Valuation (COV) Estimator",
    "Out-of-pocket cash needed after loan, CPF and grants");

do
{
    Section("Flat pricing");
    decimal purchasePrice = ReadDecimal("Agreed purchase price, S$", 1m, 10_000_000m);
    decimal valuation = ReadDecimal("HDB valuation, S$", 1m, 10_000_000m);

    Section("Financing");
    int loanChoice = ReadChoice("Loan type",
        "HDB concessionary loan (75% LTV, 2.60% p.a.)",
        "Bank loan (75% LTV, 3.50% p.a. assumed, min. 5% cash)");
    Console.WriteLine();
    Console.WriteLine("  Typical first-timer SC/SC family grants: CPF Housing Grant $80k (2-4 room) or $50k (5-room+),");
    Console.WriteLine("  EHG up to $120k, Proximity Grant $20k-$30k. Amounts depend on income and eligibility.");
    decimal grants = ReadDecimal("Total CPF housing grants you qualify for, S$ (0 if none)", 0m, 500_000m);
    decimal cpfOa = ReadDecimal("CPF OA balance available for the flat, S$ (0 if none)", 0m, 10_000_000m);

    var (loanName, ltv, annualRate, minCashRatio) = loanChoice switch
    {
        1 => ("HDB concessionary loan", 0.75m, 0.026m, 0m),
        _ => ("Bank loan", 0.75m, 0.035m, 0.05m)
    };

    decimal cov = Math.Max(0m, purchasePrice - valuation);
    decimal lowerValue = Math.Min(purchasePrice, valuation);
    decimal loanAmount = lowerValue * ltv;
    decimal downpayment = lowerValue - loanAmount;

    decimal minCash = lowerValue * minCashRatio;
    decimal grantEligibleDownpayment = downpayment - minCash;
    decimal grantsApplied = Math.Min(grants, grantEligibleDownpayment);
    decimal cpfApplied = Math.Min(cpfOa, grantEligibleDownpayment - grantsApplied);
    decimal cashForDownpayment = downpayment - grantsApplied - cpfApplied;
    decimal totalCash = cov + cashForDownpayment;
    decimal unusedGrants = grants - grantsApplied;

    decimal monthlyRate = annualRate / 12m;
    int months = LoanTenureYears * 12;
    decimal monthlyInstalment = loanAmount * monthlyRate /
        (1m - (decimal)Math.Pow((double)(1m + monthlyRate), -months));

    var (covStatus, covColor) = (purchasePrice - valuation) switch
    {
        > 0m => ($"COV of {Money(cov)} ({cov / valuation * 100m:0.0}% above valuation)", ConsoleColor.Yellow),
        0m => ("Price equals valuation (no COV)", ConsoleColor.Green),
        _ => ("Price is below valuation (no COV)", ConsoleColor.Green)
    };

    Section("Valuation");
    Row("Agreed purchase price", Money(purchasePrice));
    Row("HDB valuation", Money(valuation));
    Row("Lower of price and valuation", Money(lowerValue));
    Console.ForegroundColor = covColor;
    Console.WriteLine($"  => {covStatus}");
    Console.ResetColor();

    Section($"Financing - {loanName}");
    Row($"Maximum loan ({ltv * 100m:0}%)", Money(loanAmount));
    Row($"Downpayment ({(1m - ltv) * 100m:0}%)", Money(downpayment));
    Row("  less CPF housing grants", Money(grantsApplied));
    Row("  less CPF OA savings used", Money(cpfApplied));
    Row("  Cash for downpayment", Money(cashForDownpayment));

    Section("Out-of-pocket cash required");
    Row("Cash over valuation (COV)", Money(cov));
    Row("Cash for downpayment", Money(cashForDownpayment));
    Console.WriteLine("  " + new string('=', 50));
    Row("TOTAL CASH REQUIRED", Money(totalCash), ConsoleColor.Green);
    Console.WriteLine();
    Row($"Est. monthly instalment ({LoanTenureYears} yrs)", Money(monthlyInstalment), labelWidth: 34, valueWidth: 16);

    if (unusedGrants > 0m)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"  Note: {Money(unusedGrants)} of grants is not needed for the downpayment and would stay in");
        Console.WriteLine("  your CPF OA (it can be used for monthly instalments).");
        Console.ResetColor();
    }

    Console.WriteLine();
    Console.WriteLine("  Rules applied: loan and CPF are based on the lower of price and valuation; COV must be paid in");
    Console.WriteLine("  cash. Bank loans need at least 5% cash, and the 3.5% bank rate is an assumption. HDB loans may");
    Console.WriteLine("  require CPF OA use beyond the $20,000 each buyer may retain. Excludes stamp duty, legal, valuation");
    Console.WriteLine("  and agent fees, lease-based LTV reductions, and MSR/TDSR limits.");
}
while (AskYesNo("Estimate another purchase?"));

Console.WriteLine();
Console.WriteLine("  Thank you for using the COV estimator. Good luck with your home hunt!");

static void Banner(string title, string subtitle)
{
    string line = new('=', Math.Max(title.Length, subtitle.Length) + 6);
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine(line);
    Console.WriteLine($"   {title}");
    Console.WriteLine($"   {subtitle}");
    Console.WriteLine(line);
    Console.ResetColor();
}

static void Section(string title)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"--- {title} ".PadRight(64, '-'));
    Console.ResetColor();
}

static void Row(string label, string value, ConsoleColor? color = null, int labelWidth = 32, int valueWidth = 18)
{
    Console.Write($"  {label.PadRight(labelWidth)}");
    if (color is { } c)
    {
        Console.ForegroundColor = c;
    }
    Console.WriteLine(value.PadLeft(valueWidth));
    Console.ResetColor();
}

static string Money(decimal value) => "$" + value.ToString("N2", CultureInfo.InvariantCulture);

static void ShowError(string message)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"    ! {message}");
    Console.ResetColor();
}

static string ReadLine(string prompt)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write($"  > {prompt}: ");
    Console.ResetColor();
    string? line = Console.ReadLine();
    if (line is null)
    {
        Console.WriteLine();
        Environment.Exit(0);
    }
    return line.Trim();
}

static decimal ReadDecimal(string label, decimal min, decimal max)
{
    while (true)
    {
        string input = ReadLine($"{label} [{min:N0} - {max:N0}]").TrimStart('$');
        if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value)
            && value >= min && value <= max)
        {
            return value;
        }
        ShowError($"Please enter an amount between {min:N0} and {max:N0}.");
    }
}

static int ReadInt(string label, int min, int max)
{
    while (true)
    {
        if (int.TryParse(ReadLine(label), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
            && value >= min && value <= max)
        {
            return value;
        }
        ShowError($"Please enter a whole number between {min} and {max}.");
    }
}

static int ReadChoice(string label, params string[] options)
{
    Console.WriteLine($"  {label}:");
    for (int i = 0; i < options.Length; i++)
    {
        Console.WriteLine($"    {i + 1}) {options[i]}");
    }
    return ReadInt($"Select 1-{options.Length}", 1, options.Length);
}

static bool AskYesNo(string label)
{
    Console.WriteLine();
    while (true)
    {
        switch (ReadLine($"{label} (y/n)").ToLowerInvariant())
        {
            case "y" or "yes": return true;
            case "n" or "no": return false;
            default: ShowError("Please answer y or n."); break;
        }
    }
}

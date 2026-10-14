using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

/// <summary>
/// Same rounding as the desktop form: tax rupees use .NET midpoint-to-even,
/// running totals use midpoint away from zero.
/// </summary>
public static class DomesticInvoiceMath
{
    public const int QtyDigits = 3;
    public const int RateDigits = 2;
    public const int AmountDigits = 2;
    public const string OelCompany = "Oswal Extrusion Limited (Unit-II)";

    public static void Calculate(DomesticInvoiceDto invoice)
    {
        var gst = DateTime.TryParse(invoice.InvDate, out var invDate) && invDate.Date > new DateTime(2017, 6, 30);
        if (gst)
            CalculateGst(invoice);
        else
            CalculateLegacy(invoice);
    }

    public static void CalculateGst(DomesticInvoiceDto invoice)
    {
        var oel = invoice.CompanyName == OelCompany;
        decimal goods = 0;
        decimal taxable = 0;
        decimal cgst = 0;
        decimal sgst = 0;
        decimal igst = 0;
        decimal maxRate = 0;

        foreach (var line in invoice.Lines)
        {
            if (string.IsNullOrWhiteSpace(line.Commodity) || line.Qty == 0 || line.Rate == 0)
                continue;
            if (oel && (string.IsNullOrWhiteSpace(line.Deptt) || string.IsNullOrWhiteSpace(line.Uom)))
                continue;

            line.Qty = RoundAway(line.Qty, QtyDigits);
            line.Rate = RoundAway(line.Rate, RateDigits);
            var baseAmt = RoundAway(line.Qty * line.Rate, AmountDigits);
            goods = RoundAway(goods + baseAmt, AmountDigits);

            decimal lineTotal = baseAmt;
            if (oel)
            {
                var basic = RoundEven(baseAmt * invoice.OelBasic / 100m, 0);
                var cvd = RoundEven(basic * invoice.OelCvd / 100m, 0);
                var e1 = RoundEven((cvd + basic) * invoice.OelCess1 / 100m, 0);
                var s1 = RoundEven((cvd + basic) * invoice.OelShe1 / 100m, 0);
                var sum1 = basic + cvd + e1 + s1;
                var add = RoundEven((goods + sum1) * invoice.OelAddDuty / 100m, 0);
                sum1 += add;
                var e2 = RoundEven(sum1 * invoice.OelCess2 / 100m, 0);
                var s2 = RoundEven(sum1 * invoice.OelShe2 / 100m, 0);
                var sum2 = sum1 + e2 + s2;
                var e3 = RoundEven(sum2 * invoice.OelCess3 / 100m, 0);
                var s3 = RoundEven(sum2 * invoice.OelShe3 / 100m, 0);
                lineTotal = baseAmt + sum2 + e3 + s3;
            }

            line.Amount = lineTotal;
            taxable = RoundAway(taxable + lineTotal, AmountDigits);
            line.CgstAmt = RoundAway(lineTotal * line.CgstRate / 100m, AmountDigits);
            line.SgstAmt = RoundAway(lineTotal * line.SgstRate / 100m, AmountDigits);
            line.IgstAmt = RoundAway(lineTotal * line.IgstRate / 100m, AmountDigits);
            cgst += line.CgstAmt;
            sgst += line.SgstAmt;
            igst += line.IgstAmt;
            maxRate = Math.Max(maxRate, Math.Max(line.CgstRate, Math.Max(line.SgstRate, line.IgstRate)));
        }

        decimal total;
        if (oel)
        {
            if (invoice.GstOnSpDuty1) AddDutyGst(invoice.SpDutyAmt1, ref cgst, ref sgst, ref igst, maxRate);
            if (invoice.GstOnSpDuty2) AddDutyGst(invoice.SpDutyAmt2, ref cgst, ref sgst, ref igst, maxRate);
            if (invoice.GstOnSpDuty3) AddDutyGst(invoice.SpDutyAmt3, ref cgst, ref sgst, ref igst, maxRate);
            total = RoundAway(taxable + cgst + sgst + igst, AmountDigits);
            if (invoice.GstAfterFreight) total += invoice.Freight;
            total += invoice.SpDutyAmt1 + invoice.SpDutyAmt2 + invoice.SpDutyAmt3;
            total += RoundEven(total * invoice.TcsPercent / 100m, 0);
            if (!invoice.GstAfterFreight) total += invoice.Freight;
        }
        else
        {
            total = goods + invoice.Freight;
            if (invoice.GstAfterFreight)
                AddDutyGst(invoice.Freight, ref cgst, ref sgst, ref igst, maxRate);
            if (invoice.GstOnSpDuty1) AddDutyGst(invoice.SpDutyAmt1, ref cgst, ref sgst, ref igst, maxRate);
            if (invoice.GstOnSpDuty2) AddDutyGst(invoice.SpDutyAmt2, ref cgst, ref sgst, ref igst, maxRate);
            if (invoice.GstOnSpDuty3) AddDutyGst(invoice.SpDutyAmt3, ref cgst, ref sgst, ref igst, maxRate);
            total = RoundAway(total + cgst + sgst + igst, AmountDigits);
            total += invoice.SpDutyAmt1 + invoice.SpDutyAmt2 + invoice.SpDutyAmt3;
            total += RoundAway(total * invoice.TcsPercent / 100m, AmountDigits);
        }

        invoice.GrandTotal = RoundAway(total, AmountDigits);
    }

    public static void CalculateLegacy(DomesticInvoiceDto invoice)
    {
        var oel = invoice.CompanyName == OelCompany;
        decimal goods = 0;
        foreach (var line in invoice.Lines)
        {
            if (string.IsNullOrWhiteSpace(line.Commodity) || line.Qty == 0 || line.Rate == 0)
                continue;
            line.Qty = RoundAway(line.Qty, QtyDigits);
            line.Rate = RoundAway(line.Rate, RateDigits);
            line.Amount = RoundAway(line.Qty * line.Rate, AmountDigits);
            goods = RoundAway(goods + line.Amount, AmountDigits);
        }

        goods = RoundEven(goods, 0);
        decimal total = goods;
        if (oel)
        {
            var basic = RoundEven(goods * invoice.OelBasic / 100m, 0);
            var cvd = RoundEven((goods + basic) * invoice.OelCvd / 100m, 0);
            var e1 = RoundEven((cvd + basic) * invoice.OelCess1 / 100m, 0);
            var s1 = RoundEven((cvd + basic) * invoice.OelShe1 / 100m, 0);
            var sum1 = basic + cvd + e1 + s1;
            var add = RoundEven((goods + sum1) * invoice.OelAddDuty / 100m, 0);
            sum1 += add;
            var e2 = RoundEven(sum1 * invoice.OelCess2 / 100m, 0);
            var s2 = RoundEven(sum1 * invoice.OelShe2 / 100m, 0);
            var sum2 = sum1 + e2 + s2;
            var e3 = RoundEven(sum2 * invoice.OelCess3 / 100m, 0);
            var s3 = RoundEven(sum2 * invoice.OelShe3 / 100m, 0);
            total = goods + sum2 + e3 + s3;
            if (invoice.GstAfterFreight) total += invoice.Freight;
            var vat = RoundEven(total * invoice.Vat / 100m, 0);
            var addVat = RoundEven(total * invoice.AddVat / 100m, 0);
            total += vat + addVat;
            if (!invoice.GstAfterFreight) total += invoice.Freight;
            var tcs = RoundAway(total * invoice.TcsPercent / 100m, AmountDigits);
            total += tcs + invoice.SpDutyAmt1 + invoice.SpDutyAmt2 + invoice.SpDutyAmt3;
        }
        else
        {
            var excise = RoundEven(goods * invoice.ExDuty / 100m, 0);
            var edu = RoundEven(invoice.EduCess * excise / 100m, 0);
            var she = RoundEven(invoice.SheCess * excise / 100m, 0);
            if (invoice.GstAfterFreight) total += invoice.Freight;
            var vatBase = invoice.ExciseApplicable == "NIL" ? total : total + excise + edu + she;
            var vat = RoundEven(vatBase * invoice.Vat / 100m, 0);
            var addVat = RoundEven(vatBase * invoice.AddVat / 100m, 0);
            var exciseOn = invoice.ExciseApplicable != "NIL";
            var vatOn = invoice.VatApplicable != "NIL";
            if (!exciseOn && vatOn) total += vat + addVat;
            else if (exciseOn && !vatOn) total += edu + she + excise;
            else if (exciseOn && vatOn) total += edu + she + vat + addVat + excise;
            if (!invoice.GstAfterFreight) total += invoice.Freight;
            total += RoundAway(total * invoice.TcsPercent / 100m, AmountDigits);
        }

        invoice.GrandTotal = RoundAway(total, AmountDigits);
    }

    private static void AddDutyGst(decimal duty, ref decimal cgst, ref decimal sgst, ref decimal igst, decimal rate)
    {
        var extra = RoundAway(duty * rate / 100m, AmountDigits);
        if (cgst > 0) cgst += extra;
        if (sgst > 0) sgst += extra;
        if (igst > 0) igst += extra;
    }

    public static decimal RoundAway(decimal value, int digits) =>
        Math.Round(value, digits, MidpointRounding.AwayFromZero);

    public static decimal RoundEven(decimal value, int digits) =>
        Math.Round(value, digits, MidpointRounding.ToEven);

    public static string RupeesInWords(decimal amount)
    {
        var n = (long)Math.Truncate(amount);
        if (n == 0) return "Zero";
        if (n < 0) return "Minus " + RupeesInWords(-n);
        string[] ones =
        [
            "", "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten",
            "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen"
        ];
        string[] tens = ["", "", "Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy", "Eighty", "Ninety"];

        static string Two(long v, string[] ones, string[] tens)
        {
            if (v < 20) return ones[v];
            var t = tens[v / 10];
            var o = ones[v % 10];
            return o.Length == 0 ? t : t + " " + o;
        }

        var parts = new List<string>();
        void Push(long value, string label)
        {
            if (value <= 0) return;
            parts.Add(Two(value, ones, tens) + (label.Length == 0 ? "" : " " + label));
        }

        Push(n / 10000000, "Crore");
        n %= 10000000;
        Push(n / 100000, "Lakh");
        n %= 100000;
        Push(n / 1000, "Thousand");
        n %= 1000;
        Push(n / 100, "Hundred");
        n %= 100;
        if (n > 0) parts.Add(Two(n, ones, tens));
        return string.Join(" ", parts).Trim();
    }
}

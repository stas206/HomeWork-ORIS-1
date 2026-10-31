namespace OrdersApp;

public static class Store
{
    public static List<Order> Orders = new();
    public static int NextId = 1;

    public static void Reset()
    {
        Orders.Clear();
        NextId = 1;
    }

    public static bool CustomerOk(string s)
        => s != null && s.Trim().Length >= 2 && s.Trim().Length <= 60;

    public static bool AmountOk(int a) => a >= 1 && a <= 100000;

    public static bool StatusOk(string s) => s == "unpaid" || s == "paid";

    public static Order Create(string customer, int amount)
    {
        if (!CustomerOk(customer)) throw new ArgumentException("customer");
        if (!AmountOk(amount)) throw new ArgumentException("amount");

        var o = new Order
        {
            Id = NextId++,
            Customer = customer.Trim(),
            Amount = amount,
            Status = "unpaid"
        };
        Orders.Add(o);
        return o;
    }

    public static Order Find(int id) => Orders.FirstOrDefault(o => o.Id == id);

    public static List<Order> Filter(int? minAmount)
    {
        if (minAmount == null) return Orders.ToList();
        return Orders.Where(o => o.Amount >= minAmount.Value).ToList();
    }

    public static void Update(int id, string customer, int amount, string status)
    {
        var o = Find(id) ?? throw new KeyNotFoundException();

        if (!CustomerOk(customer)) throw new ArgumentException("customer");
        if (!AmountOk(amount)) throw new ArgumentException("amount");
        if (!StatusOk(status)) throw new ArgumentException("status");

        o.Customer = customer.Trim();
        o.Amount = amount;
        o.Status = status;
    }
}
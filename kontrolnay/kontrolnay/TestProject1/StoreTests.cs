using Microsoft.VisualStudio.TestTools.UnitTesting; 
using OrdersApp;                                    
using System;                                        
using System.Collections.Generic;

namespace TestProject1;

[TestClass]
public class StoreTests
{
    [TestInitialize]
    public void Init() => Store.Reset();

    [TestMethod]
    public void Create_SetsUnpaidAndAssignsId()
    {
        var o = Store.Create("Иван", 500);
        Assert.AreEqual(1, o.Id);
        Assert.AreEqual("unpaid", o.Status);
        Assert.AreEqual(500, o.Amount);
    }

    [TestMethod]
    public void Create_ThrowsOnInvalidData()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Store.Create("A", 100));
        Assert.ThrowsExactly<ArgumentException>(() => Store.Create("Иван", 0));
        Assert.ThrowsExactly<ArgumentException>(() => Store.Create("Иван", 100001));
    }

    [TestMethod]
    public void Filter_ByMinAmount()
    {
        Store.Create("A", 100);
        Store.Create("B", 500);
        Store.Create("C", 1000);

        var filtered = Store.Filter(500);
        Assert.AreEqual(2, filtered.Count);
        Assert.AreEqual(3, Store.Filter(null).Count);
    }

    [TestMethod]
    public void Update_ChangesAllFields()
    {
        var o = Store.Create("A", 100);
        Store.Update(o.Id, "B", 999, "paid");

        var fresh = Store.Find(o.Id);
        Assert.AreEqual("B", fresh.Customer);
        Assert.AreEqual(999, fresh.Amount);
        Assert.AreEqual("paid", fresh.Status);
    }
    
    [TestMethod]
    public void Update_ThrowsWhenNotFound()
    {
        Assert.ThrowsExactly<KeyNotFoundException>(() => Store.Update(999, "A", 100, "paid"));
    }
}
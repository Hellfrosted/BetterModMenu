using BetterModMenu.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BetterModMenu.Tests;

[TestClass]
public class WorkshopTagCacheTests
{
    [TestMethod]
    public void RequestsOneFetchAndPublishesTagsForEachMod()
    {
        var completion = new TaskCompletionSource<Dictionary<string, List<string>>>();
        int calls = 0;
        var cache = new WorkshopTagCacheCoordinator(ids => { calls++; return completion.Task; });
        var installed = new[] { ("first", "123"), ("second", "123") };
        cache.Request(installed);
        cache.Request(installed);
        Assert.AreEqual(1, calls);
        Assert.AreEqual(0, cache.Tags.Count);
        Assert.IsFalse(cache.TryPublish(out _));
        completion.SetResult(new() { ["123"] = ["QoL"] });
        Assert.IsTrue(cache.TryPublish(out var error), error);
        CollectionAssert.AreEqual(new[] { "QoL" }, cache.Tags["first"]);
        CollectionAssert.AreEqual(new[] { "QoL" }, cache.Tags["second"]);
        cache.Request(installed);
        Assert.AreEqual(1, calls);
    }

    [TestMethod]
    public void ResetAndChangedInstallationsDiscardStaleResults()
    {
        var first = new TaskCompletionSource<Dictionary<string, List<string>>>();
        var second = new TaskCompletionSource<Dictionary<string, List<string>>>();
        int calls = 0;
        var cache = new WorkshopTagCacheCoordinator(_ => ++calls == 1 ? first.Task : second.Task);
        cache.Request([("old", "1")]);
        cache.Reset();
        cache.Request([("new", "2")]);
        first.SetResult(new() { ["1"] = ["Old"] });
        Assert.IsFalse(cache.TryPublish(out _));
        second.SetResult(new() { ["2"] = ["New"] });
        Assert.IsTrue(cache.TryPublish(out _));
        Assert.IsFalse(cache.Tags.ContainsKey("old"));
        CollectionAssert.AreEqual(new[] { "New" }, cache.Tags["new"]);
    }

    [TestMethod]
    public void FailureRequiresExplicitRetry()
    {
        int calls = 0;
        var cache = new WorkshopTagCacheCoordinator(_ => ++calls == 1
            ? Task.FromException<Dictionary<string, List<string>>>(new IOException("offline"))
            : Task.FromResult(new Dictionary<string, List<string>> { ["1"] = ["Tools"] }));
        cache.Request([("mod", "1")]);
        Assert.IsFalse(cache.TryPublish(out var error));
        Assert.AreEqual("offline", error);
        cache.Request([("mod", "1")]);
        Assert.AreEqual(1, calls);
        cache.Request([("mod", "1")], retryFailure: true);
        Assert.IsTrue(cache.TryPublish(out _));
        Assert.AreEqual(2, calls);
    }

    [TestMethod]
    public void InstallationChangeDiscardsAnOlderInFlightResult()
    {
        var oldFetch = new TaskCompletionSource<Dictionary<string, List<string>>>();
        var newFetch = new TaskCompletionSource<Dictionary<string, List<string>>>();
        int calls = 0;
        var cache = new WorkshopTagCacheCoordinator(_ => ++calls == 1 ? oldFetch.Task : newFetch.Task);
        cache.Request([("old", "1")]);
        Assert.IsTrue(cache.Request([("new", "2")]));
        newFetch.SetResult(new() { ["2"] = ["New"] });
        Assert.IsTrue(cache.TryPublish(out _));
        oldFetch.SetResult(new() { ["1"] = ["Old"] });
        Assert.IsFalse(cache.TryPublish(out _));
        Assert.IsFalse(cache.Tags.ContainsKey("old"));
        CollectionAssert.AreEqual(new[] { "New" }, cache.Tags["new"]);
    }

    [TestMethod]
    public void SeededCacheIsReusedUntilInstallationsChange()
    {
        int calls = 0;
        var cache = new WorkshopTagCacheCoordinator(_ => { calls++; return Task.FromResult(new Dictionary<string, List<string>>()); });
        cache.Seed([("mod", "1")], new Dictionary<string, List<string>> { ["mod"] = ["Tools"] });
        Assert.IsFalse(cache.Request([("mod", "1")], retryFailure: true));
        Assert.AreEqual(0, calls);
        Assert.IsTrue(cache.Request([]));
        Assert.AreEqual(0, cache.Tags.Count);
        Assert.AreEqual(0, calls);
    }
}

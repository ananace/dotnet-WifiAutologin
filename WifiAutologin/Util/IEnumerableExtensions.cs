namespace WifiAutologin.Util;

public static class IEnumerableExtensions
{
    public static async Task<IEnumerable<T>> Where<T>(this IEnumerable<T> source, Func<T, Task<bool>> predicate)
    {
        var results = await Task.WhenAll(source.Select(async x => (x, await predicate(x))));
        return results.Where(x => x.Item2).Select(x => x.Item1);
    }
}

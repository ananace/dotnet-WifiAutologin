namespace WifiAutologin.Util;

public static class GroupCollectionExtensions
{
    public static IDictionary<string, string> AsDictionary(this System.Text.RegularExpressions.GroupCollection groups)
    {
        var enumerable = groups as IEnumerable<System.Text.RegularExpressions.Group>;
        return enumerable.ToDictionary(k => k.Name, v => v.Value);
    }
}

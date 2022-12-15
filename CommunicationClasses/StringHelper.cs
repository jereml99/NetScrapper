namespace DefaultNamespace;

public static class StringHelper
{
    public static string ToHumanBytes(this long len)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1) {
            order++;
            len = len/1024;
        }
        return  String.Format("{0:0.##} {1}", len, sizes[order]);
    }
}
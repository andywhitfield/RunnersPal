namespace RunnersPal.Web.Extensions
{
    public static class StringExtensions
    {
        public static string MaxLength(this string str, int maxLength)
        {
            if (str == null) return str;
            if (str.Length <= maxLength) return str;
            return str.Substring(0, maxLength);
        }
    }
}
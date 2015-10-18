using System.IO;

namespace RubberDuckyApp
{
    public static class SanitizeHelper
    {
        public static string SanitizeForFile(this string value)
        {
            return string.Concat(value.Split(Path.GetInvalidFileNameChars()));
        }
    }
}
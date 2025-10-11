namespace Aisix.Common
{
    public class Log
    {
        public static bool Enable { get; set; }

        public static void WriteLine(ConsoleColor color = ConsoleColor.White, string msg = "")
        {
            if (!Enable) { return; }

            Console.ForegroundColor = color;
            Console.WriteLine($"{DateTime.Now} {msg}");
            Console.ResetColor();
        }

        public static void Write(ConsoleColor color = ConsoleColor.White, string msg = "")
        {
            if (!Enable) { return; }

            Console.ForegroundColor = color;
            Console.Write($"{DateTime.Now} {msg}");
            Console.ResetColor();
        }
    }
}

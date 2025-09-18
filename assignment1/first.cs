using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;

namespace FirstOpenTK
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            var native = new NativeWindowSettings
            {
                ClientSize = new Vector2i(800, 600),
                Title = "My First OpenTK"
            };

            using var game = new Game(GameWindowSettings.Default, native);
            game.Run();
        }
    }
}
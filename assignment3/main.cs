using OpenTK.Windowing.Desktop;
using OpenTK.Mathematics;

class main
{
    static void Main(string[] args)
    {
        var gws = GameWindowSettings.Default;
        var nws = new NativeWindowSettings()
        {
            ClientSize = new Vector2i(1024, 768),
            Title = "3D Cube in OpenTK (All C#)"
        };

        using var game = new cube(gws, nws);
        game.Run();
    }
}

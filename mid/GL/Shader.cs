using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.IO;

namespace midtermGame.Graphics
{
    // Thin wrapper around OpenGL shader program creation.
    // Responsibilities:
    // - Load GLSL files from Shaders folder
    // - Compile vertex and fragment shaders from source
    // - Link into a program
    // - Provide a small cache for uniform locations

    public sealed class Shader : IDisposable
    {
        public int Handle { get; private set; }
        private readonly Dictionary<string, int> _uniforms = new();

        // NEW convenience method that loads shaders directly from files
        public static Shader FromFiles(string vertexFileName = "vert.glsl", string fragmentFileName = "frag.glsl")
        {
            string vertexPath = Path.Combine(AppContext.BaseDirectory, "Shaders", vertexFileName);
            string fragmentPath = Path.Combine(AppContext.BaseDirectory, "Shaders", fragmentFileName);

            if (!File.Exists(vertexPath))
                throw new FileNotFoundException($"Vertex shader file not found: {vertexPath}");
            if (!File.Exists(fragmentPath))
                throw new FileNotFoundException($"Fragment shader file not found: {fragmentPath}");

            string vertexSrc = File.ReadAllText(vertexPath);
            string fragmentSrc = File.ReadAllText(fragmentPath);

            return FromSource(vertexSrc, fragmentSrc);
        }

        public static Shader FromSource(string vertexSrc, string fragmentSrc)
        {
            var vs = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vs, vertexSrc);
            GL.CompileShader(vs);
            GL.GetShader(vs, ShaderParameter.CompileStatus, out int okV);
            if (okV == 0) throw new Exception(GL.GetShaderInfoLog(vs));

            var fs = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fs, fragmentSrc);
            GL.CompileShader(fs);
            GL.GetShader(fs, ShaderParameter.CompileStatus, out int okF);
            if (okF == 0) throw new Exception(GL.GetShaderInfoLog(fs));

            var prog = GL.CreateProgram();
            GL.AttachShader(prog, vs);
            GL.AttachShader(prog, fs);
            GL.LinkProgram(prog);
            GL.GetProgram(prog, GetProgramParameterName.LinkStatus, out int okP);
            if (okP == 0) throw new Exception(GL.GetProgramInfoLog(prog));

            GL.DetachShader(prog, vs);
            GL.DetachShader(prog, fs);
            GL.DeleteShader(vs);
            GL.DeleteShader(fs);

            return new Shader { Handle = prog };
        }

        public void Use() => GL.UseProgram(Handle);

        public int GetUniformLocation(string name)
        {
            if (_uniforms.TryGetValue(name, out var loc)) return loc;
            loc = GL.GetUniformLocation(Handle, name);
            _uniforms[name] = loc;
            return loc;
        }

        public void Dispose()
        {
            if (Handle != 0) GL.DeleteProgram(Handle);
            Handle = 0;
        }
    }
}

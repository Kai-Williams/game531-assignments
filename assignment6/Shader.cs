using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;


namespace OpenTKFpsCamera
{
    public class Shader : IDisposable
    {
        public int Handle { get; private set; }


        public Shader(string vertPath, string fragPath)
        {
            string vertexSource = File.ReadAllText(vertPath);
            string fragmentSource = File.ReadAllText(fragPath);

            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexSource);
            GL.CompileShader(vertexShader);
            GL.GetShader(vertexShader, ShaderParameter.CompileStatus, out int vStatus);
            if (vStatus == 0)
                throw new Exception($"Vertex shader compile error: {GL.GetShaderInfoLog(vertexShader)}");

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentSource);
            GL.CompileShader(fragmentShader);
            GL.GetShader(fragmentShader, ShaderParameter.CompileStatus, out int fStatus);
            if (fStatus == 0)
                throw new Exception($"Fragment shader compile error: {GL.GetShaderInfoLog(fragmentShader)}");

            Handle = GL.CreateProgram();
            GL.AttachShader(Handle, vertexShader);
            GL.AttachShader(Handle, fragmentShader);
            GL.LinkProgram(Handle);
            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int linkStatus);
            if (linkStatus == 0)
                throw new Exception($"Program link error: {GL.GetProgramInfoLog(Handle)}");

            GL.DetachShader(Handle, vertexShader);
            GL.DetachShader(Handle, fragmentShader);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }


        public void Use() => GL.UseProgram(Handle);
        public int GetUniformLocation(string name) => GL.GetUniformLocation(Handle, name);


        public void Dispose()
        {
            if (Handle != 0)
            {
                GL.DeleteProgram(Handle);
                Handle = 0;
            }
        }
    }
}
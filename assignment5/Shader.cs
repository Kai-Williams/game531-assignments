using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace PhongOpenTK
{
    public class Shader : IDisposable
    {
        public int Handle { get; private set; }

        public Shader(string vertPath, string fragPath)
        {
            string vertexCode = File.ReadAllText(vertPath);
            string fragmentCode = File.ReadAllText(fragPath);

            int v = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(v, vertexCode);
            GL.CompileShader(v);
            GL.GetShader(v, ShaderParameter.CompileStatus, out int vStatus);
            if (vStatus == 0)
                throw new Exception($"Vertex shader error:\n{GL.GetShaderInfoLog(v)}");

            int f = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(f, fragmentCode);
            GL.CompileShader(f);
            GL.GetShader(f, ShaderParameter.CompileStatus, out int fStatus);
            if (fStatus == 0)
                throw new Exception($"Fragment shader error:\n{GL.GetShaderInfoLog(f)}");

            Handle = GL.CreateProgram();
            GL.AttachShader(Handle, v);
            GL.AttachShader(Handle, f);
            GL.LinkProgram(Handle);
            GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out int pStatus);
            if (pStatus == 0)
                throw new Exception($"Program link error:\n{GL.GetProgramInfoLog(Handle)}");

            GL.DetachShader(Handle, v);
            GL.DetachShader(Handle, f);
            GL.DeleteShader(v);
            GL.DeleteShader(f);
        }

        public void Use() => GL.UseProgram(Handle);

        public void SetMatrix4(string name, Matrix4 mat)
        {
            int loc = GL.GetUniformLocation(Handle, name);
            GL.UniformMatrix4(loc, false, ref mat);
        }

        public void SetMatrix3(string name, Matrix3 mat)
        {
            int loc = GL.GetUniformLocation(Handle, name);
            GL.UniformMatrix3(loc, false, ref mat);
        }

        public void SetVector3(string name, Vector3 v)
        {
            int loc = GL.GetUniformLocation(Handle, name);
            GL.Uniform3(loc, v);
        }

        public void SetFloat(string name, float f)
        {
            int loc = GL.GetUniformLocation(Handle, name);
            GL.Uniform1(loc, f);
        }

        public void Dispose()
        {
            GL.DeleteProgram(Handle);
        }
    }
}

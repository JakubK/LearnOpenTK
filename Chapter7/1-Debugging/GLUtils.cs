using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL4;

namespace LearnOpenTK;

public static class GLUtils
{
    public static ErrorCode CheckError(
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        ErrorCode error;
        while ((error = GL.GetError()) != ErrorCode.NoError)
        {
            string errorString = error switch
            {
                ErrorCode.InvalidEnum => "INVALID_ENUM",
                ErrorCode.InvalidValue => "INVALID_VALUE",
                ErrorCode.InvalidOperation => "INVALID_OPERATION",
                // ErrorCode.StackOverflow => "STACK_OVERFLOW",
                // ErrorCode.StackUnderflow => "STACK_UNDERFLOW",
                ErrorCode.OutOfMemory => "OUT_OF_MEMORY",
                ErrorCode.InvalidFramebufferOperation => "INVALID_FRAMEBUFFER_OPERATION",
                _ => error.ToString()
            };

            Console.WriteLine("GL.GetError():");
            Console.WriteLine($"{errorString} | {file} ({line})");
        }

        return error;
    }
    
    public static DebugProc DebugCallback = DebugMessage;

    private static void DebugMessage(
        DebugSource source,
        DebugType type,
        int id,
        DebugSeverity severity,
        int length,
        IntPtr message,
        IntPtr userParam)
    {
        // ignore non-significant errors
        if (id == 131169 || id == 131185 || id == 131218 || id == 131204)
            return;

        var msg = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(message);

        Console.WriteLine("---------------");
        Console.WriteLine($"Debug message ({id}): {msg}");
        
        Console.WriteLine($"Source: {source}");
        Console.WriteLine($"Type: {type}");
        Console.WriteLine($"Severity: {severity}");
        Console.WriteLine();
    }
}
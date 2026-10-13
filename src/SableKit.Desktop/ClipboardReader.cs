using System.Runtime.InteropServices;

namespace SableKit.Desktop;

// Read only when requested. A delayed-rendering clipboard owner must not block the UI.
internal static class ClipboardReader
{
    private static int reading;
    internal static async Task<string> ReadTextAsync()
    {
        if (Interlocked.CompareExchange(ref reading, 1, 0) != 0) throw Busy();
        var pending = Task.Run(() =>
        {
            try { return ReadUnicode(); }
            finally { Interlocked.Exchange(ref reading, 0); }
        });
        try { return await pending.WaitAsync(TimeSpan.FromMilliseconds(250)); }
        catch (TimeoutException)
        {
            _ = pending.ContinueWith(t => { _ = t.Exception; }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
            throw Busy();
        }
    }
    private static string ReadUnicode()
    {
        if (!OpenClipboard(IntPtr.Zero)) throw Busy();
        try
        {
            const uint unicodeText = 13;
            if (!IsClipboardFormatAvailable(unicodeText)) return "";
            var handle = GetClipboardData(unicodeText);
            if (handle == IntPtr.Zero) throw Busy();
            var pointer = GlobalLock(handle);
            if (pointer == IntPtr.Zero) throw Busy();
            try
            {
                // Network detection has a 16K-character text budget; retain an extra character to reject excess.
                var characters = (int)Math.Min(GlobalSize(handle).ToUInt64() / 2, 16385UL);
                var text = Marshal.PtrToStringUni(pointer, characters) ?? "";
                var end = text.IndexOf('\0');
                return end < 0 ? text : text[..end];
            }
            finally { GlobalUnlock(handle); }
        }
        finally { CloseClipboard(); }
    }
    private static COMException Busy() => new("Clipboard is busy. Try again.");
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool OpenClipboard(IntPtr owner);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool CloseClipboard();
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsClipboardFormatAvailable(uint format);
    [DllImport("user32.dll")] private static extern IntPtr GetClipboardData(uint format);
    [DllImport("kernel32.dll")] private static extern IntPtr GlobalLock(IntPtr memory);
    [DllImport("kernel32.dll")] private static extern UIntPtr GlobalSize(IntPtr memory);
    [DllImport("kernel32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool GlobalUnlock(IntPtr memory);
}

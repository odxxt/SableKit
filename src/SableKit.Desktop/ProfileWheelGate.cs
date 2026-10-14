namespace SableKit.Desktop;

internal sealed class ProfileWheelGate
{
    private long lastStep = long.MinValue / 2;
    private int remainder;
    internal int Step(int delta, long now)
    {
        if (delta == 0) return 0;
        if (now - lastStep < 420) { remainder = 0; return 0; }
        if (Math.Sign(remainder) != Math.Sign(delta)) remainder = 0;
        remainder += Math.Clamp(delta, -120, 120);
        if (Math.Abs(remainder) < 120) return 0;
        var direction = remainder < 0 ? 1 : -1;
        remainder = 0; lastStep = now;
        return direction;
    }
}

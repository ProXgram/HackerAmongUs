using System;

namespace HAU;

/// <summary>Consumes the release at one GUI-close position for at most two frames.</summary>
public sealed class NativePointerReleaseGuard
{
    private int _closeFrame = -1;
    private float _x, _y;
    private const float PositionTolerance = 2f;

    public void ConsumeCloseRelease(int frame, float x, float y)
    {
        if (frame < 0 || !float.IsFinite(x) || !float.IsFinite(y)) return;
        _closeFrame = frame;
        _x = x;
        _y = y;
    }

    public bool Blocks(int frame, float x, float y)
    {
        if (_closeFrame < 0) return false;
        long elapsed = (long)frame - _closeFrame;
        if (elapsed < 0 || elapsed > 1)
        {
            _closeFrame = -1;
            return false;
        }
        return float.IsFinite(x) && float.IsFinite(y) &&
            Math.Abs(x - _x) <= PositionTolerance && Math.Abs(y - _y) <= PositionTolerance;
    }
}

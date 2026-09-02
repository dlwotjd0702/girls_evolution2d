using System;
using System.Collections.Generic;

/// <summary>Rolling one-second click rate, with sustained charge and a short decay grace.</summary>
public sealed class FeverMeter
{
    readonly Queue<double> clicks = new Queue<double>();
    public int RequiredClicksPerSecond = 3;
    public double ChargeSeconds = 3;
    public double DecayPerSecond = 2;
    public double Charge { get; private set; }
    public bool IsActive { get; private set; }
    public int ClicksPerSecond => clicks.Count;
    public float ChargeFill => (float)Math.Min(1, Charge / Math.Max(0.1, ChargeSeconds));
    // Taps give a small immediate visual acknowledgement. This never grants fever;
    // the actual bonus still requires Charge to reach ChargeSeconds.
    public float TapPreviewFill => IsActive ? 1f : (float)Math.Min(0.15,
        clicks.Count / (double)Math.Max(1, RequiredClicksPerSecond) * 0.15);
    public float Fill => Math.Max(ChargeFill, TapPreviewFill);

    public void RecordClick(double now)
    {
        Prune(now);
        clicks.Enqueue(now);
    }

    public void Tick(double now, double delta)
    {
        Prune(now);
        // A focus/resume gap cannot count as sustained tapping.
        delta = Math.Max(0, Math.Min(0.1, delta));
        if (clicks.Count >= Math.Max(1, RequiredClicksPerSecond))
            Charge = Math.Min(Math.Max(0.1, ChargeSeconds), Charge + delta);
        else Charge = Math.Max(0, Charge - delta * Math.Max(0, DecayPerSecond));
        if (Charge >= Math.Max(0.1, ChargeSeconds)) IsActive = true;
        if (Charge <= 0) IsActive = false;
    }

    void Prune(double now)
    {
        while (clicks.Count > 0 && now - clicks.Peek() >= 1) clicks.Dequeue();
    }

    public void Reset() { clicks.Clear(); Charge = 0; IsActive = false; }
}

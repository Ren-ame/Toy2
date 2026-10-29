using System;

public interface IGauge
{
    float Ratio { get; }
    float Max { get; }
    event Action<float, float> OnChanged;
}
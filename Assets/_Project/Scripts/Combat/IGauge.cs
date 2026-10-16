using System;

public interface IGauge
{
    float Ratio { get; }
    event Action<float, float> OnChanged;
}
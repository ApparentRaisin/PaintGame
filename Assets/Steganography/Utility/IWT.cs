using Unity.Mathematics;
using UnityEngine;

public struct ColorInt
{
    public int r;
    public int g;
    public int b;

    public ColorInt(Color color, float multip = 255f) : this(color.r, color.g, color.b, multip) { }

    public ColorInt(int r, int g, int b)
    {
        this.r = r;
        this.g = g;
        this.b = b;
    }
    public ColorInt(float r, float g, float b, float multip = 255f)
    {
        this.r = (int)math.floor(r * multip);
        this.g = (int)math.floor(g * multip);
        this.b = (int)math.floor(b * multip);
    }

    public ColorInt(double r, double g, double b, double multip = 255.0)
    {
        this.r = (int)math.floor(r * multip);
        this.g = (int)math.floor(g * multip);
        this.b = (int)math.floor(b * multip);
    }

    public Color ToColor()
    {
        return new Color(r / 255f, g / 255f, b / 255f);
    }
    public float3 ToFloat3()
    {
        return new float3(r / 255f, g / 255f, b / 255f);
    }

    public static ColorInt operator +(ColorInt a, ColorInt b)
    {
        return new ColorInt(a.r + b.r, a.g + b.g, a.b + b.b);
    }
    public static ColorInt operator -(ColorInt a, ColorInt b)
    {
        return new ColorInt(a.r - b.r, a.g - b.g, a.b - b.b);
    }
    public static ColorInt operator /(ColorInt a, int value)
    {
        return new ColorInt(a.r / value, a.g / value, a.b / value);
    }
    public static ColorInt operator *(ColorInt a, int value)
    {
        return new ColorInt(a.r * value, a.g * value, a.b * value);
    }
    public static ColorInt operator *(int value, ColorInt a)
    {
        return new ColorInt(a.r * value, a.g * value, a.b * value);
    }

    public override string ToString()
    {
        return $"({r}, {g}, {b})";
    }
}

public class IWT
{
    public static void Test()
    {
        ColorInt[] c = new ColorInt[16];
        for (int i = 0; i < c.Length; i++)
            c[i] = new ColorInt(i, i, i);
 
        IWT53(c, out var highFrequencyBands, out var lowFrequencyBands);
        //Debug.Log(c);
        LogArray(highFrequencyBands);
        LogArray(lowFrequencyBands);
         for (int i = 0; i < highFrequencyBands.Length; i++)
        {
            if (highFrequencyBands[i].r % 2 == 0)
                highFrequencyBands[i].r++;
            if (highFrequencyBands[i].g % 2 == 0)
                highFrequencyBands[i].g++;
            if (highFrequencyBands[i].b % 2 == 0)
                highFrequencyBands[i].b++;
        }
        LogArray(highFrequencyBands);
        LogArray(lowFrequencyBands);
        c = InverseIWT53(highFrequencyBands, lowFrequencyBands);
        Debug.LogWarning(c);

        Debug.Log("---- 第二轮 ----");

        IWT53(c, out highFrequencyBands, out lowFrequencyBands);
        LogArray(highFrequencyBands);
        LogArray(lowFrequencyBands);
        c = InverseIWT53(highFrequencyBands, lowFrequencyBands);
        Debug.LogWarning(c);

    }

    static void LogArray(ColorInt[] array)
    {
        string log = $"[{array.Length}] ";
        for (int i = 0; i < array.Length; i++)
            log += $"{array[i]}, ";
        Debug.Log(log);
    }

    public static void IWT53(ColorInt[] colors, out ColorInt[] highFrequencyBands, out ColorInt[] lowFrequencyBands)
    {
        int length = colors.Length / 2;
        var s = new ColorInt[length];
        var d = new ColorInt[length];

        for (int i = 0; i < length; i++)
        {
            s[i] = colors[i * 2];
            d[i] = colors[i * 2 + 1];
        }

        var a = new ColorInt[length - 1];
        var b = new ColorInt[length - 1];
        for (int i = 0; i < length - 1; i++)
        {
            a[i].r = d[i].r - Mathf.FloorToInt(0.5f * (s[i].r + s[i + 1].r));
            a[i].g = d[i].g - Mathf.FloorToInt(0.5f * (s[i].g + s[i + 1].g));
            a[i].b = d[i].b - Mathf.FloorToInt(0.5f * (s[i].b + s[i + 1].b));
        }

        b[0] = d[length - 1] - s[length - 1];
        for (int i = 0; i < length - 1; i++)
            d[i] = a[i];
        d[length - 1] = b[0];

        for (int i = 1; i < length; i++)
        {
            b[i - 1].r = s[i].r + Mathf.FloorToInt(0.25f * (d[i].r + d[i - 1].r) + 0.5f);
            b[i - 1].g = s[i].g + Mathf.FloorToInt(0.25f * (d[i].g + d[i - 1].g) + 0.5f);
            b[i - 1].b = s[i].b + Mathf.FloorToInt(0.25f * (d[i].b + d[i - 1].b) + 0.5f);
        }

        s[0].r += Mathf.FloorToInt(0.5f * d[0].r + 0.5f);
        s[0].g += Mathf.FloorToInt(0.5f * d[0].g + 0.5f);
        s[0].b += Mathf.FloorToInt(0.5f * d[0].b + 0.5f);
        for (int i = 1; i < length; i++)
            s[i] = b[i - 1];

        highFrequencyBands = s;
        lowFrequencyBands = d;
    }

    public static ColorInt[] InverseIWT53(ColorInt[] highFrequencyBands, ColorInt[] lowFrequencyBands)
    {
        var s = highFrequencyBands;
        var d = lowFrequencyBands;
        int length = s.Length;

        var a = new ColorInt[length - 1];
        var b = new ColorInt[length - 1];

        for (int i = 1; i < length; i++)
        {
            b[i - 1].r = s[i].r - Mathf.FloorToInt(0.25f * (d[i].r + d[i - 1].r) + 0.5f);
            b[i - 1].g = s[i].g - Mathf.FloorToInt(0.25f * (d[i].g + d[i - 1].g) + 0.5f);
            b[i - 1].b = s[i].b - Mathf.FloorToInt(0.25f * (d[i].b + d[i - 1].b) + 0.5f);
        }
        //LogArray(b);

        s[0].r += Mathf.FloorToInt(0.5f * d[0].r + 0.5f);
        s[0].g += Mathf.FloorToInt(0.5f * d[0].g + 0.5f);
        s[0].b += Mathf.FloorToInt(0.5f * d[0].b + 0.5f);
        for (int i = 1; i < length; i++)
            s[i] = b[i - 1];
        //LogArray(s);

        for (int i = 0; i < length - 1; i++)
        {
            a[i].r = d[i].r + Mathf.FloorToInt(0.5f * (s[i].r + s[i + 1].r));
            a[i].g = d[i].g + Mathf.FloorToInt(0.5f * (s[i].g + s[i + 1].g));
            a[i].b = d[i].b + Mathf.FloorToInt(0.5f * (s[i].b + s[i + 1].b));
        }
        //LogArray(a);
        b[0] = d[length - 1] + s[length - 1];
        for (int i = 0; i < length - 1; i++)
            d[i] = a[i];
        d[length - 1] = b[0];
        //LogArray(d);

        ColorInt[] colors = new ColorInt[length * 2];
        for (int i = 0; i < length; i++)
            colors[i * 2] = s[i];
        for (int i = 0; i < length; i++)
            colors[i * 2 + 1] = d[i];
        return colors;
    }
}


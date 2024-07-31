namespace CocoaPlugin.API;

public class Time
{
    public int Hours { get; init; }
    public int Minutes { get; init; }
    public int Seconds { get; init; }

    public override string ToString()
    {
        return $"{Hours:D1}시간 {Minutes:D2}분 {Seconds:D2}초";
    }

    public static Time operator -(Time a, Time b)
    {
        var hours = a.Hours - b.Hours;
        var minutes = a.Minutes - b.Minutes;
        var seconds = a.Seconds - b.Seconds;

        if (seconds < 0)
        {
            seconds += 60;
            minutes--;
        }

        if (minutes < 0)
        {
            minutes += 60;
            hours--;
        }

        return new Time
        {
            Hours = hours,
            Minutes = minutes,
            Seconds = seconds
        };
    }

    public static Time operator +(Time a, Time b)
    {
        var hours = a.Hours + b.Hours;
        var minutes = a.Minutes + b.Minutes;
        var seconds = a.Seconds + b.Seconds;

        if (seconds >= 60)
        {
            seconds -= 60;
            minutes++;
        }

        if (minutes >= 60)
        {
            minutes -= 60;
            hours++;
        }

        return new Time
        {
            Hours = hours,
            Minutes = minutes,
            Seconds = seconds
        };
    }
}
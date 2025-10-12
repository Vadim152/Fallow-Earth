using System;
using UnityEngine;

[Flags]
public enum ColonistScheduleActivityMask
{
    None = 0,
    Work = 1 << 0,
    Sleep = 1 << 1,
    Recreation = 1 << 2,
    Medical = 1 << 3,
    Any = Work | Sleep | Recreation | Medical
}

public enum ColonistScheduleActivity
{
    Work,
    Sleep,
    Recreation,
    Medical
}

[Serializable]
public class ColonistSchedule
{
    [SerializeField]
    private ColonistScheduleActivity[] hours = new ColonistScheduleActivity[24];

    public ColonistSchedule()
    {
        for (int i = 0; i < hours.Length; i++)
            hours[i] = ColonistScheduleActivity.Work;
    }

    public ColonistSchedule(ColonistScheduleActivity defaultActivity)
    {
        for (int i = 0; i < hours.Length; i++)
            hours[i] = defaultActivity;
    }

    public ColonistScheduleActivity this[int hour]
    {
        get => hours[Mathf.Clamp(hour, 0, 23)];
        set => hours[Mathf.Clamp(hour, 0, 23)] = value;
    }

    public ColonistScheduleActivity GetActivityForHour(int hour)
    {
        if (hour < 0)
            hour = 0;
        hour %= 24;
        return hours[hour];
    }

    public void SetRange(int startHour, int endHour, ColonistScheduleActivity activity)
    {
        if (startHour < 0) startHour = 0;
        if (endHour > 24) endHour = 24;
        for (int h = startHour; h < endHour; h++)
            hours[h % 24] = activity;
    }

    public ColonistSchedule Clone()
    {
        ColonistSchedule copy = new ColonistSchedule();
        for (int i = 0; i < hours.Length; i++)
            copy.hours[i] = hours[i];
        return copy;
    }

    public ColonistScheduleActivityMask ToMask(int hour)
    {
        switch (GetActivityForHour(hour))
        {
            case ColonistScheduleActivity.Sleep:
                return ColonistScheduleActivityMask.Sleep;
            case ColonistScheduleActivity.Recreation:
                return ColonistScheduleActivityMask.Recreation;
            case ColonistScheduleActivity.Medical:
                return ColonistScheduleActivityMask.Medical;
            default:
                return ColonistScheduleActivityMask.Work;
        }
    }

    public ColonistScheduleActivity[] ToArray()
    {
        var copy = new ColonistScheduleActivity[hours.Length];
        Array.Copy(hours, copy, hours.Length);
        return copy;
    }

    public void LoadFrom(ColonistScheduleActivity[] data)
    {
        if (data == null)
            return;
        int len = Mathf.Min(hours.Length, data.Length);
        for (int i = 0; i < len; i++)
            hours[i] = data[i];
    }
}

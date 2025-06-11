using UnityEngine;

/// <summary>
/// Task for moving to another colonist and socializing to fill the social need.
/// </summary>
public class SocializeTask : TimedTask
{
    public Colonist partner;

    public SocializeTask(Colonist partner, float duration, System.Action<Colonist> onComplete = null)
        : base(partner.transform.position, duration, onComplete)
    {
        this.partner = partner;
    }
}

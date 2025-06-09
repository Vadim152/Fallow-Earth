using UnityEngine;

public class Task
{
    public Vector2 target;
    public System.Action<Colonist> onComplete;

    public Task(Vector2 target, System.Action<Colonist> onComplete = null)
    {
        this.target = target;
        this.onComplete = onComplete;
    }

    public void Complete(Colonist c)
    {
        if (onComplete != null)
            onComplete(c);
    }
}

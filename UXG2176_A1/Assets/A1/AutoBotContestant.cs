using Assignment1;
using UnityEngine;

public class AutoBotContestant : MonoBehaviour
{
    public AutoBotController autobot;
    private AutoBot ab;

    public virtual void Ready(AutoBot enemy, Color color)
    {
        ab.Ready(enemy, color);
    }

    public void Init()
    {
        ab = GetComponent<AutoBot>();
        autobot = GetComponent<AutoBotController>();
    }

    public AutoBot GetAutoBot()
    {
        return ab;
    }

    public void Clear()
    {
        ab.Clear();
    }

    public bool IsDead()
    {
        return ab.IsDead();
    }

    public int GetHP()
    {
        return ab.HP;
    }

    public int GetMaxHP()
    {
        return AutoBot.MaxHP;
    }
}

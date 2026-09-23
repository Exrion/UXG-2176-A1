using Assignment1;
using UnityEngine;

namespace CaiXuanAutoBot
{
    class CaiXuanAutoBot: AutoBotContestant
    {
        CaiXuanAutoBotBT bitBotBT;

        public override void Ready(AutoBot enemy, Color color)
        {
            base.Ready(enemy, color);
            bitBotBT = new CaiXuanAutoBotBT();
            bitBotBT.Start(autobot);
        }

        public void Update()
        {
            bitBotBT.Update();
        }
    }
}
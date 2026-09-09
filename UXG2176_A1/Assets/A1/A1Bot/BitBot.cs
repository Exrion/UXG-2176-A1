using Assignment1;
using BitBotBehaviourTree;
using UnityEngine;

namespace A1.A1AutoBot
{
    class BitBot: AutoBotContestant
    {
        BitBotBT bitBotBT;

        public override void Ready(AutoBot enemy, Color color)
        {
            base.Ready(enemy, color);
            bitBotBT = new BitBotBT();
            bitBotBT.Start(autobot);
        }

        public void Update()
        {
            bitBotBT.Update();
        }
    }
}
using UnityEngine;

namespace Assignment1
{
    public abstract class AutoBotState
    {
        protected AutoBotController controller;
        
        public AutoBotState(AutoBotController controller)
        {
            //store all fields
            this.controller = controller;
        }

        public abstract void DoActionUpdate(float dt);
    }
}
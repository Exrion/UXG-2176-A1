using Assignment1;
using System.Collections.Generic;
using UnityEngine;

namespace BehaviourTree
{
    public abstract class Tree 
    {
        private Node root;

        public void Start(AutoBotController autobot)
        {
            root = SetupTree();
            root.SetData("controller", autobot);
        }

        public void Update()
        {
            if (root != null)
            {
                root.Evaluate();
            }
        }

        protected abstract Node SetupTree();
    }
}
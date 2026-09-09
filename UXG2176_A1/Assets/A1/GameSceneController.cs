using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assignment1
{
    public class GameSceneController : MonoBehaviour
    {
        public string sceneName;
        protected GameController gameController;

        public virtual void Initialize(GameController aController)
        {
            gameController = aController;
        }
    }
}
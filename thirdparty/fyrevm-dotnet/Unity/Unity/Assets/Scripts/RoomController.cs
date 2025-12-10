using UnityEngine;
using UnityEngine.UI;

namespace FyreVMDemo.Game
{
    public class RoomController : MonoBehaviour
    {
        public GameObject RootGameObj;

        protected void Start()
        {
            TransitionController.Instance.AddToHiearchy(RootGameObj);
        }

        /// <summary>
        /// Method for telling inform to go in a certain direction.
        /// </summary>
        /// <param name="sDirection"></param>
        public void GoToDirection(string sDirection)
        {
            GlulxStateService.Instance.GoToDirection(sDirection);
        }
    }
}
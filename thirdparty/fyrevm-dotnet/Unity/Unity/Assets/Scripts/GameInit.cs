using UnityEngine;
using UnityEngine.UI;

namespace FyreVMDemo.Game
{
    public class GameInit : MonoBehaviour
    {
        public TextAsset ulxFile;

        protected void Start()
        {
            GlulxStateService.Instance.Initialize(ulxFile);
            GlulxStateService.Instance.InitialScene();
        }
    }
}
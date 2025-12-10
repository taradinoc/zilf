using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FyreVMDemo.Game
{
    public class TransitionController : MonoBehaviour
    {
        #region Singleton Pattern

        protected static TransitionController _instance;

        public static TransitionController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = GameObject.Find("TransitionController").GetComponent<TransitionController>();
                }

                return _instance;
            }
        }
        #endregion

        private List<GameObject> Hierarchy = new List<GameObject>();

        /// <summary>
        /// Adds the root of the hierarchy to the hierarchy collection.
        /// </summary>
        /// <param name="obj"></param>
        public void AddToHiearchy(GameObject obj)
        {
            Hierarchy.Add(obj);
        }

        /// <summary>
        /// Loads the level specified
        /// </summary>
        /// <param name="sLevelID"></param>
        public void Transition(string sLevelID)
        {
            StartCoroutine(LoadNextLevel(sLevelID));
        }

        /// <summary>
        /// Coroutine to load the next level and to destroy and previous levels that were loaded (by destroying the root obj of that scene)
        /// </summary>
        /// <param name="sLevelID"></param>
        /// <returns></returns>
        private IEnumerator LoadNextLevel(string sLevelID)
        {
            Application.LoadLevelAdditive(sLevelID);

            yield return null;

            var listCopy = new List<GameObject>(Hierarchy);
            foreach (var obj in listCopy)
            {
                if(obj.name.ToUpperInvariant() != sLevelID.ToUpperInvariant())
                {
                    Hierarchy.Remove(obj);
                    Destroy(obj);
                }
            }
        }
    }
}
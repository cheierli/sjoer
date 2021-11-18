﻿using UnityEngine;
using Assets.InfoItems;
using Assets.Positional;
using Assets.Resources;
using Assets.DataManagement;
using Assets.Graphics;
using UnityEngine.SceneManagement;

namespace Assets.SceneManagement
{
    public class DefaultScene : MonoBehaviour
    {
        [SerializeField]
        public GameObject player;

        Player aligner;

        private InfoCategory[] infoCategories;
        private InfoItem[] allInfoItems;

        void Start()
        {
            Player aligner = player.GetComponent<Player>();
            GraphicFactory.Instance.aligner ??= aligner;

            infoCategories = new InfoCategory[1]
            {
                new DelayedInfoCategory(
                    DataConnections.BarentswatchAIS, DataAdapters.BarentswatchAIS, ParameterExtractors.BarentswatchAIS, 
                    aligner, 
                    DataType.AIS, DisplayArea.HorizonPlane,
                    (float) Config.Instance.conf.DataSettings["UpdateInterval"])
            };
        }

        void Update()
        {
            foreach (InfoCategory infoCategory in infoCategories) {
                infoCategory.Update();
            }
        }

        void OnApplicationQuit()
        {
            OnDestroy();
        }

        void OnDisable()
        {
            OnDestroy();
        }

        void OnEnable()
        {
            SceneManager.activeSceneChanged += OnSceneLoaded;
        }

        void OnSceneLoaded(Scene o, Scene i)
        {
            OnDestroy();
        }

        void OnDestroy()
        {
            if (infoCategories != null)
            {
                foreach (InfoCategory i in infoCategories)
                {
                    i.OnDestroy();
                }

            }
        }
    }
}

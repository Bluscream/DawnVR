﻿using UnityEngine;
using System.Linq;
using UnityStandardAssets._1CC59503E;

namespace DawnVR.Modules.VR
{
    internal class VRRig : MonoBehaviour
    {
        public static VRRig Instance;
        public VRCamera Camera;
        public VRInput Input;

        private Camera mainSceneCam;
        private T_C3DD66D9 cachedChloe;

        private void Start()
        {
            // todo: for future me, look at the main scene camera and look into the components, there is something there that allows rendering those object highlights
            DontDestroyOnLoad(gameObject);
            Camera = transform.Find("Camera").gameObject.AddComponent<VRCamera>();
            Input = new VRInput();
        }

        public void UpdateRigParent(eGameMode gameMode)
        {
            switch (gameMode)
            {
                case eGameMode.kCustomization:
                    throw new System.NotImplementedException();
                case eGameMode.kCutscene:
                    // todo: doesnt follow the camera properly
                    // me in the future - cause i am blind and this is using a simple position set instead of parenting the camera
                    //transform.position = ((Camera)typeof(T_C3DD66D9).Assembly.GetType("T_34182F31").GetProperty("main").GetValue(null, null))?.transform.position ?? Vector3.zero;
                    //SetParent(((Camera)typeof(T_C3DD66D9).Assembly.GetType("T_34182F31").GetProperty("main").GetValue(null, null))?.transform);

                    // set camera's rendertexture to the one used for the ui to prevent motion sickness
                    SetParent(cachedChloe.transform);
                    break;
                case eGameMode.kDialog:
                    throw new System.NotImplementedException();
                case eGameMode.kFreeRoam:
                    // SetParent(((Transform)typeof(_1EB728BCC.T_A7E3390E).GetField("_123859A1E", HarmonyLib.AccessTools.all).GetValue(cachedChloe.gameObject.GetComponent<_1EB728BCC.T_A7E3390E>())).parent, new Vector3(0, -1, 0));
                    break;
                case eGameMode.kLoading:
                    break;
                case eGameMode.kMainMenu:
                    transform.rotation = Quaternion.Euler(0, 85, 0);
                    transform.position = ((Camera)typeof(T_C3DD66D9).Assembly.GetType("T_34182F31").GetProperty("main").GetValue(null, null))?.transform.position ?? Vector3.zero;
                    break;
                case eGameMode.kNone:
                    break;
                case eGameMode.kPosterView:
                    throw new System.NotImplementedException();
                case eGameMode.kVideo:
                    throw new System.NotImplementedException();
            }
        }

        private void SetParent(Transform t, Vector3? newLocalPosition = null, bool resetPos = true)
        {
            transform.parent = t;
            if (resetPos)
            {
                if (newLocalPosition.HasValue)
                    transform.localPosition = newLocalPosition.Value;
                else
                    transform.localPosition = Vector3.zero;
            }
        }

        public void UpdateCachedChloe(T_C3DD66D9 newChloe, bool updateParent = true)
        {
            cachedChloe = newChloe;
            if (updateParent)
                UpdateRigParent(T_A6E913D1.Instance.m_gameModeManager.CurrentMode);
        }
    }
}
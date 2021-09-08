﻿using System;
using Valve.VR;
using System.Linq;
using UnityEngine;
using MelonLoader;
using System.Reflection;
using DawnVR.Modules.VR;
using UnityEngine._1F1547F66;

namespace DawnVR.Modules
{
    // todo: possibly separate patches by function (some moved to VRRig, VRCamera, etc)
    internal static class HarmonyPatches
    {
        private static HarmonyLib.Harmony HarmonyInstance;

        private static void PatchPre(MethodInfo original, string prefixMethodName) => HarmonyInstance.Patch(original, typeof(HarmonyPatches).GetMethod(prefixMethodName).ToNewHarmonyMethod());
        private static void PatchPost(MethodInfo original, string postfixMethodName) => HarmonyInstance.Patch(original, null, typeof(HarmonyPatches).GetMethod(postfixMethodName).ToNewHarmonyMethod());

        public static void Init(HarmonyLib.Harmony hInstance)
        {
            // todo: find cause of input enabling delay, im sure its from one of these patches
            HarmonyInstance = hInstance;
            // Debug Stuff
            PatchPost(typeof(T_EDB11480).GetMethod("StartSplash"), nameof(DisableSplashScreen)); // skips most of the splash screens
            PatchPre(typeof(T_BF5A5EEC).GetMethod("SkipPressed"), nameof(CutsceneSkipPressed)); // allows skipping any cutscene

            // Input Handling
            PatchPre(typeof(T_6FCAE66C).GetMethod("_1B350D183", HarmonyLib.AccessTools.all), nameof(InputManagerInit)); // makes the game think we're using an xbox controller
            PatchPre(typeof(T_C3DD66D9).GetMethod("CalculateAngle"), nameof(CalculateCharAngle)); // overrides it so it doesnt actually calculate the angle, as VRRig and CharControllerMove handles that
            PatchPre(typeof(T_6FCAE66C).GetMethod("GetInputState", new Type[] { typeof(eGameInput), typeof(bool), typeof(bool), typeof(bool) }), nameof(GetInputState_Enum)); // redirect input to vr controllers
            PatchPre(typeof(T_D9E8342E).GetMethod("GetButtonState"), nameof(GetButtonState)); // redirect input to vr controllers
            PatchPre(typeof(T_D9E8342E).GetMethod("GetAxis"), nameof(GetAxis)); // redirect input to vr controllers

            // Rig Parent Updating
            PatchPre(typeof(T_91FF9D92).GetMethod("UnloadCurrentLevel"), nameof(UnloadCurrentLevel)); // prevents the vrrig from getting destroyed after unloading a scene
            PatchPost(typeof(T_6B664603).GetMethod("SetMode"), nameof(OnSetMode)); // lets VRRig know when the game's mode changes

            // Objective Manager
            PatchPost(typeof(T_81803C2C).GetMethod("SetReminder"), nameof(SetReminderTexture)); // adds the reminder to the vr hands
            PatchPre(typeof(T_1928221C).GetMethod("Update", HarmonyLib.AccessTools.all), nameof(DontRunMe)); // makes it so the objective view button doesnt work since it's useless in vr

            // Highlight Manager
            PatchPre(typeof(T_244D769F).GetMethod("Interact"), nameof(HotspotObjectInteract)); // prevents some weird bug
            PatchPre(typeof(T_1C1609D7).GetMethod("Update"), nameof(CUICameraRelativeUpdate)); // overrides the ui3d camera with the vrcamera rotation
            PatchPre(typeof(T_2D9F19A8).GetMethod("UpdatePosition"), nameof(CUIAnchorUpdatePosition)); // makes it so OverlayPosition isnt needed
            PatchPre(typeof(T_F8FE3E1C).GetMethod("Update"), nameof(DontRunMe)); // honestly can't remember why this is here
            PatchPre(typeof(T_8F74F848).GetMethod("CheckOnScreen"), nameof(IsHotspotOnScreen)); // HotSpotUI, makes it use the vr camera for calculations
            PatchPre(typeof(T_572A4969).GetMethod("CheckOnScreen"), nameof(IsInteractOnScreen)); // InteractUI, makes it use the vr camera for calculations
            PatchPre(typeof(T_A0A6EA62).GetMethod("CheckOnScreen"), nameof(IsHoverObjectOnScreen)); // HoverObjectUI, makes it use the vr camera for calculations

            // Tutorial Fixes
            PatchPre(typeof(T_64B68373).GetMethod("SetTutorial"), nameof(SetTutorialInfo)); // fixes the issue after disabling the objective reminder button

            // Misc
            PatchPost(typeof(T_C3DD66D9).GetMethod("Start"), nameof(PostCharControllerStart)); // mainly updates VRRig's chloe and material
            PatchPre(typeof(T_96E81635).GetProperty("ScrollingText").GetGetMethod(), nameof(ReplaceScrollingText)); // adds a personal touch lol
            PatchPost(typeof(T_421B9CDF).GetMethod("SetCameraPosition"), nameof(SetCameraPosition)); // moves VRRig to follow the camera during a cutscene
            PatchPre(typeof(T_3BE79CFB).GetMethod("Start", HarmonyLib.AccessTools.all), nameof(BoundaryStart)); // prevents a bug with the boundaries
            PatchPre(typeof(T_3BE79CFB).GetMethod("OnTriggerEnter", HarmonyLib.AccessTools.all), nameof(DontRunMe)); // part 2 of the boundary issue fix
            PatchPost(typeof(T_884A92DB).GetMethod("Start"), nameof(FollowCamStart)); // prevents bug with FollowCamera disabling interaction
            PatchPre(typeof(T_884A92DB).GetMethod("LateUpdate"), nameof(FollowCamLateUpdate)); // part 2 of FollowCamera fix
            PatchPre(typeof(T_C3DD66D9).GetMethod("Move"), nameof(CharControllerMove)); // improves the game's movement controller to better fit vr
            PatchPre(typeof(T_55EA835B).GetMethod("Awake", HarmonyLib.AccessTools.all), nameof(MirrorReflectionAwake)); // overrides the mirror component with a modified one made for vr
            PatchPre(typeof(T_408CFC35).GetMethod("UpdateFade"), nameof(UpdateUIFade)); // makes fades use SteamVR_Fade instead of a transition window
            // post processing doesnt seem to render correctly in vr, so this is gonna stay disabled
            //PatchPre(typeof(T_190FC323).GetMethod("OnEnable", HarmonyLib.AccessTools.all), nameof(OnPPEnable));
        }

        #region Debug Stuff

        public static bool CutsceneSkipPressed(T_BF5A5EEC __instance)
        {
            _15C6DD6D9.T_58A5E6E2 currentMode = __instance.GetCurrentMode<_15C6DD6D9.T_58A5E6E2>();
            if (currentMode != null)
            {
                T_156BDACC timeline = T_14474339.GetTimeline(currentMode);
                if (timeline != null)
                {
                    float sequenceEndTime = currentMode.sequenceEndTime;
                    float timeS = sequenceEndTime - timeline.CurrentTime;
                    T_E8819104.Singleton.AdvanceAllSounds(timeS);
                    timeline.SetTime(sequenceEndTime);
                    _169E4A3E.T_4B84CB26.s_forceFullEvaluate = true;
                    T_14474339.UpdateCurrentTimelinesForFrame();
                }
            }

            if (T_A6E913D1.Instance.m_rumbleManager != null)
                T_A6E913D1.Instance.m_rumbleManager.ClearAllRumbles(0f);

            return false;
        }

        public static void DisableSplashScreen(T_EDB11480 __instance) => __instance.m_splashList.Clear();

        #endregion

        #region Input Handling

        public static void InputManagerInit(T_6FCAE66C __instance)
            => __instance._1C6FBAE09 = eControlType.kXboxOne;

        public static bool CalculateCharAngle(T_C3DD66D9 __instance, Vector3 _13F806F29)
        {
            __instance._11C77E995 = Quaternion.Euler(0, VRRig.Instance.Camera.transform.eulerAngles.y, 0);
            if (_13F806F29 != __instance.m_moveDirection)
            {
                __instance.m_moveDirection = (__instance.m_nonNormalMoveDirection = _13F806F29);
                __instance.m_moveDirection.Normalize();
                __instance._15B7EF7A4 = Vector3.Angle(Vector3.forward, __instance.m_moveDirection);
                if (_13F806F29.x < 0f)
                    __instance._15B7EF7A4 = 360f - __instance._15B7EF7A4;
            }
            return false;
        }

        private const float speedModifier = 0.05f;
        private const float sprintModifier = 0.08f;

        public static bool CharControllerMove(T_C3DD66D9 __instance, bool _1AF4345B4)
        {
            if (_1AF4345B4)
                __instance.Rotate();
            if (__instance.m_moveDirection != Vector3.zero)
            {
                Vector3 axis = T_A6E913D1.Instance.m_inputManager.GetAxisVector3(eGameInput.kMovementXPositive, eGameInput.kNone, eGameInput.kMovementYPositive);
                float modifier = T_A6E913D1.Instance.m_inputManager.GetAxisAndKeyValue(eGameInput.kJog) == 1 ? sprintModifier : speedModifier;
                __instance.m_navAgent.Move(__instance._11C77E995 * axis * modifier);
            }
            return false;
        }

        public static eInputState GetInputState_Binding(T_6FCAE66C inputManInstance, T_9005A419 binding)
        {
            if (inputManInstance.InputBlocked)
                return eInputState.kNone;

            for (int i = 0; i < binding.m_joystick.Count; i++)
            {
                eInputState buttonState = T_D9E8342E.Singleton.GetButtonState(binding.m_joystick[i]);
                if (buttonState != eInputState.kNone)
                    return buttonState;
            }

            return eInputState.kNone;
        }

        public static bool GetInputState_Enum(T_6FCAE66C __instance, ref eInputState __result, eGameInput _1561EDFFF)
        {
            if (__instance.InputBlocked)
            {
                __result = eInputState.kNone;
                return false;
            }

            if (_1561EDFFF == eGameInput.kAny)
            {
                if (SteamVR_Input.actionsBoolean.Any((a) => a != SteamVR_Actions.default_HeadsetOnHead && a.stateDown))
                {
                    __result = eInputState.kDown;
                    return false;
                }
                if (SteamVR_Input.actionsBoolean.Any((a) => a != SteamVR_Actions.default_HeadsetOnHead && a.state))
                {
                    __result = eInputState.kHeld;
                    return false;
                }
            }
            else if (__instance.m_keyBindings.ContainsKey((int)_1561EDFFF))
            {
                T_9005A419 keybinding = __instance.m_keyBindings[(int)_1561EDFFF];
                __result = GetInputState_Binding(__instance, keybinding);
                return false;
            }

            __result = eInputState.kNone;
            return false;
        }

        public static bool GetButtonState(ref eInputState __result, eJoystickKey _13A42C455)
        {
            __result = eInputState.kNone;

            switch (_13A42C455)
            {
                case eJoystickKey.kNone:
                    break;
                case eJoystickKey.kStart:
                    __result = VRInput.GetBooleanInputState(VRRig.Instance.Input.GetButtonThumbstick(VRInput.Hand.Right));
                    break;
                case eJoystickKey.kSelect:
                    __result = VRInput.GetBooleanInputState(VRRig.Instance.Input.GetButtonThumbstick(VRInput.Hand.Left));
                    break;
                /*case eJoystickKey.kDPadUp:
                    __result = VRInput.GetBooleanInputState(VRRig.Instance.Input.GetThumbstickUp(VRInput.Hand.Left));
                    break;
                case eJoystickKey.kDPadRight:
                    __result = VRInput.GetBooleanInputState(VRRig.Instance.Input.GetThumbstickRight(VRInput.Hand.Left));
                    break;
                case eJoystickKey.kDPadLeft:
                    __result = VRInput.GetBooleanInputState(VRRig.Instance.Input.GetThumbstickLeft(VRInput.Hand.Left));
                    break;
                case eJoystickKey.kDPadDown:
                    __result = VRInput.GetBooleanInputState(VRRig.Instance.Input.GetThumbstickDown(VRInput.Hand.Left));
                    break;*/
                case eJoystickKey.kR1:
                    __result = VRInput.GetBooleanInputState(VRRig.Instance.Input.GetTrigger(VRInput.Hand.Right));
                    break;
                case eJoystickKey.kR2:
                    __result = VRInput.GetBooleanInputState(VRRig.Instance.Input.GetGrip(VRInput.Hand.Right));
                    break;
                case eJoystickKey.kR3:
                    __result = VRInput.GetBooleanInputState(VRRig.Instance.Input.GetButtonThumbstick(VRInput.Hand.Right));
                    break;
                case eJoystickKey.kL1:
                    __result = VRInput.GetBooleanInputState(VRRig.Instance.Input.GetTrigger(VRInput.Hand.Left));
                    break;
                case eJoystickKey.kL2:
                    __result = VRInput.GetBooleanInputState(VRRig.Instance.Input.GetGrip(VRInput.Hand.Left));
                    break;
                case eJoystickKey.kL3:
                    __result = VRInput.GetBooleanInputState(VRRig.Instance.Input.GetButtonThumbstick(VRInput.Hand.Left));
                    break;
                case eJoystickKey.kAction1:
                    __result = VRInput.GetBooleanInputState(VRRig.Instance.Input.GetButtonX());
                    break;
                case eJoystickKey.kAction2:
                    __result = VRInput.GetBooleanInputState(VRRig.Instance.Input.GetButtonY());
                    break;
                case eJoystickKey.kAction3:
                    __result = VRInput.GetBooleanInputState(VRRig.Instance.Input.GetButtonB());
                    break;
                case eJoystickKey.kAction4:
                    __result = VRInput.GetBooleanInputState(VRRig.Instance.Input.GetButtonA());
                    break;
                // are these used here?
                case eJoystickKey.kPlatform:
                    break;
                case eJoystickKey.kLeftStickX:
                    break;
                case eJoystickKey.kLeftStickY:
                    break;
                case eJoystickKey.kRightStickX:
                    break;
                case eJoystickKey.kRightStickY:
                    break;
                default:
                    break;
            }

            return false;
        }

        public static bool GetAxis(ref float __result, eJoystickKey _1BBA85C4E)
        {
            __result = 0;

            switch (_1BBA85C4E)
            {
                case eJoystickKey.kNone:
                    break;
                case eJoystickKey.kLeftStickX:
                    __result = VRRig.Instance.Input.GetThumbstickVector(VRInput.Hand.Left).axis.x;
                    break;
                case eJoystickKey.kLeftStickY:
                    __result = VRRig.Instance.Input.GetThumbstickVector(VRInput.Hand.Left).axis.y;
                    break;
                case eJoystickKey.kRightStickX:
                    __result = VRRig.Instance.Input.GetThumbstickVector(VRInput.Hand.Right).axis.x;
                    break;
                case eJoystickKey.kRightStickY:
                    __result = VRRig.Instance.Input.GetThumbstickVector(VRInput.Hand.Right).axis.y;
                    break;
                // are these used here?
                case eJoystickKey.kR1:
                    break;
                case eJoystickKey.kR2:
                    break;
                case eJoystickKey.kL1:
                    break;
                case eJoystickKey.kL2:
                    break;
                default:
                    break;
            }

            return false;
        }

        #endregion

        #region Rig Parent Updating

        public static void OnSetMode(bool __result, eGameMode _1C57B7248)
        {
            if (__result)
            {
                MelonLogger.Msg("Game successfully updated to mode " + _1C57B7248);
                if (_1C57B7248 == eGameMode.kFreeRoam && (VRRig.Instance.ChloeComponent == null || VRRig.Instance.ChloeComponent.enabled == false))
                    VRRig.Instance.UpdateCachedChloe(T_A6E913D1.Instance.m_mainCharacter);
                VRRig.Instance?.UpdateRigParent(_1C57B7248);
            }
        }

        public static void UnloadCurrentLevel() => VRRig.Instance.UpdateRigParent(eGameMode.kNone);

        #endregion

        #region Objective Manager

        public static void SetReminderTexture(T_81803C2C __instance)
        {
            __instance.SetAlpha(1);
            VRRig.Instance.ActiveHandRenderers[0].transform.Find("handpad").GetComponent<MeshRenderer>().sharedMaterial = __instance.m_reminderRenderer.material;
        }

        #endregion

        #region Highlight Manager

        public static bool CUICameraRelativeUpdate(T_1C1609D7 __instance)
        {
            __instance.transform.rotation = VRRig.Instance.Camera.transform.rotation;
            return false;
        }

        public static bool CUIAnchorUpdatePosition(T_2D9F19A8 __instance)
        {
            if (__instance.m_anchorObj != null)
            {
                Transform parent = __instance.transform.parent;
                __instance.transform.localPosition = ((!(parent != null)) ? __instance.m_anchorObj.transform.position : parent.InverseTransformPoint(__instance.m_anchorObj.transform.position)) + __instance.m_offset;
            }
            return false;
        }

        public static void HotspotObjectInteract(T_6FD30C1C _1BAF664A9) => _1BAF664A9.m_lookAt = null;

        public static bool IsHotspotOnScreen(T_8F74F848 __instance, ref bool __result)
        {
            if (__instance.m_anchor == null || __instance.m_anchor.m_anchorObj == null)
            {
                __result = false;
                return false;
            }

            float distance = Vector3.Distance(VRRig.Instance.Camera.transform.position, __instance.m_anchor.m_anchorObj.transform.position);
            if (distance < 20)
            {
                __instance._14888EF3 = 1f;
                __result = true;
                return false;
            }
            else
                __instance._14888EF3 = 0f;

            __result = false;
            return false;
        }

        public static bool IsInteractOnScreen(T_572A4969 __instance, ref bool __result)
        {
            if (__instance.m_anchor != null && __instance.m_anchor.m_anchorObj != null)
            {
                T_6FD30C1C hotspotObj = __instance._133075675;
                float num = Vector3.Angle(VRRig.Instance.Camera.transform.forward, __instance.m_anchor.m_anchorObj.transform.position - VRRig.Instance.Camera.transform.position);
                if (num < 90f)
                {
                    float distance = Vector3.Distance(VRRig.Instance.Camera.transform.position, __instance.m_anchor.m_anchorObj.transform.position);
                    if (distance < 3)
                    {
                        __instance.m_arrow.gameObject.SetActive(true);
                        __instance.m_choiceUI.gameObject.SetActive(true);
                        if (hotspotObj != null)
                            hotspotObj.Select(true, true);
                        __result = true;
                        return false;
                    }
                }
                __instance.m_arrow.gameObject.SetActive(false);
                __instance.m_choiceUI.gameObject.SetActive(false);
                if (hotspotObj != null)
                    hotspotObj.Select(false, false);
            }
            __result = false;
            return false;
        }

        public static bool IsHoverObjectOnScreen(T_A0A6EA62 __instance, ref bool __result)
        {
            Vector3 vector = VRRig.Instance.Camera.Component.WorldToScreenPoint(__instance.m_anchor.m_anchorObj.transform.position);
            if (vector.x > 0f && vector.y > 0f && vector.x < VRRig.Instance.Camera.Component.pixelWidth && vector.y < VRRig.Instance.Camera.Component.pixelHeight)
            {
                float num = VRRig.Instance.Camera.Component.pixelWidth * T_A0A6EA62._1D66D99B4;

                if (vector.x < num)
                    __instance._14888EF3 = Mathf.Lerp(0f, 1f, vector.x / num);
                else if (vector.x > VRRig.Instance.Camera.Component.pixelWidth - num)
                    __instance._14888EF3 = Mathf.Lerp(0f, 1f, (VRRig.Instance.Camera.Component.pixelWidth - vector.x) / num);
                else
                    __instance._14888EF3 = 1f;

                num = VRRig.Instance.Camera.Component.pixelHeight * T_A0A6EA62._1D66D99B4;

                if (vector.y < num)
                    __instance._14888EF3 *= Mathf.Lerp(0f, 1f, vector.y / num);
                else if (vector.y > VRRig.Instance.Camera.Component.pixelHeight - num)
                    __instance._14888EF3 *= Mathf.Lerp(0f, 1f, (VRRig.Instance.Camera.Component.pixelHeight - vector.y) / num);

                __instance._1649E566F();
                __result = true;
                return false;
            }
            __instance._1649E566F();
            __result = false;
            return false;
        }

        #endregion

        #region Tutorial Fixes

        public static bool SetTutorialInfo(T_64B68373 __instance, T_64B68373.eCurrentLesson _1B1E89CA4)
        {
            if (_1B1E89CA4 == T_64B68373.eCurrentLesson.kObjective)
            {
                __instance._133003896(T_64B68373.eCurrentLesson.kCloseWindow); // NextLesson
                return false;
            }

            return true;
        }

        #endregion

        #region Misc

        public static void PostCharControllerStart(T_C3DD66D9 __instance)
        {
            VRRig.Instance?.UpdateCachedChloe(__instance);

            #region Add Hand Material

            Material material = null;
            foreach (SkinnedMeshRenderer sMesh in __instance.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                material = sMesh.sharedMaterials?.SingleOrDefault((m) => m.name.Contains("Arms"));
                if (material == null)
                    material = sMesh.sharedMaterials?.SingleOrDefault((m) => m.name.Contains("Farewell_Body"));
            }
            if (material == null)
            {
                MelonLogger.Error("Failed to locate the hand material in scene " + UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                VRRig.Instance.ChloeMaterial = new Material(Shader.Find("Standard"));
                return;
            }

            material.hideFlags = HideFlags.DontUnloadUnusedAsset;
            VRRig.Instance.ChloeMaterial = material;

            #endregion
        }

        public static void FollowCamStart(T_884A92DB __instance) => __instance.enabled = false;

        public static bool FollowCamLateUpdate(T_884A92DB __instance)
        {
            if (__instance.m_haltTransitionPreemtively || (__instance.isFreeroamStart && (__instance.m_cameraStartTransitionTime == 0f || __instance.m_cameraStartMaxTransitionSpeed == 0f)))
            {
                __instance.isFreeroamStart = false;
                if (__instance.m_camera != null)
                {
                    __instance._1B2CE6DA7(1f);
                    __instance.m_camera.useDepthOfField = false;
                    __instance.m_camera.ApplyValues(T_34182F31.main);
                }
                __instance.m_haltTransitionPreemtively = false;
            }
            if (__instance.Character == null) return false;

            __instance._13A040863 = __instance.Character.transform.position;
            if (__instance.m_isCameraDriver && __instance.m_camera != null)
            {
                if (__instance.isFreeroamStart)
                {
                    if (!__instance.m_isInteractionBlocked && __instance.m_startTransitionLerp == 0f)
                    {
                        __instance.specificTween = 0;
                        __instance._1A6D21309 = __instance.m_camera.localPosition;
                        __instance._1CD4518D7 = __instance.m_camera.localRotation;
                        if (__instance.m_camera.useDepthOfField)
                            __instance.m_initialStartDOFAperture = __instance.m_camera.aperture;
                        else
                            __instance.m_initialStartDOFAperture = 0f;
                        __instance._1C5B95284 = Vector3.Distance(__instance._1A6D21309, __instance._1B0A2CC1E);
                    }

                    __instance.m_startTransitionLerp = Mathf.SmoothDamp(__instance.m_startTransitionLerp, 1f, ref __instance._18EAA3360, __instance.m_cameraStartTransitionTime / 5f, float.PositiveInfinity, __instance._19FA60FD3());
                    __instance._1A6D21309 = Vector3.SmoothDamp(__instance._1A6D21309, __instance._1B0A2CC1E, ref __instance.m_cameraTransitionVel, __instance.m_cameraStartTransitionTime / 5f, __instance.m_cameraStartMaxTransitionSpeed, __instance._19FA60FD3());
                    if (__instance._1C5B95284 > 0.01f)
                        __instance.currentTransitionLerp = Mathf.Min(__instance.m_startTransitionLerp, 1f - (__instance._1A6D21309 - __instance._1B0A2CC1E).magnitude / __instance._1C5B95284);
                    else
                        __instance.currentTransitionLerp = __instance.m_startTransitionLerp;
                    __instance._1B2CE6DA7(__instance.currentTransitionLerp);
                    __instance.m_camera.ApplyValues(T_34182F31.main);
                    T_34182F31.main.transform.position = __instance._1A6D21309;
                    T_34182F31.main.transform.rotation = Quaternion.Slerp(__instance._1CD4518D7, __instance._13B9C8A95, __instance.currentTransitionLerp);
                    if (__instance.currentTransitionLerp - 0.999f >= 0f)
                    {
                        __instance.isFreeroamStart = false;
                        __instance.m_freeroamStartTransitionOverride = false;
                        __instance.m_isInteractionBlocked = false;
                        __instance.m_camera.focalLength = __instance._16DBB8DE;
                        __instance.m_camera.useDepthOfField = false;
                        __instance.m_startTransitionLerp = 0f;
                        __instance.currentTransitionLerp = 0f;
                        __instance.m_cameraStartTransitionTime = 0f;
                        __instance.m_cameraStartMaxTransitionSpeed = 500f;
                    }
                }
                else if (__instance.specificTween > 0)
                {
                    __instance.m_exitTweenLerp = Mathf.SmoothDamp(__instance.m_exitTweenLerp, 1f, ref __instance._18EAA3360, __instance.m_exitTweenTime / 5f, float.PositiveInfinity, __instance._19FA60FD3());
                    Quaternion rotation = T_34182F31.main.transform.rotation;
                    if (__instance.specificTween == 1)
                        rotation = Quaternion.Slerp(__instance.m_exitTweenStartOrientation, __instance.m_exitTweenGoalOrientation, __instance.m_exitTweenLerp);
                    else if (__instance.specificTween == 2)
                        rotation = Quaternion.Slerp(__instance.m_exitTweenGoalOrientation, Quaternion.identity, __instance.m_exitTweenLerp);
                    if (__instance.m_exitTweenLerp - 0.999 >= 0.0)
                    {
                        __instance.m_exitTweenLerp = 0f;
                        if (__instance.specificTween == 1)
                            __instance.specificTween = -1;
                        else
                            __instance.specificTween = 0;
                        if (__instance._1CFF88EAF != null)
                        {
                            __instance._1CFF88EAF();
                            __instance._1CFF88EAF = null;
                        }
                    }
                    __instance.m_camera.ApplyValues(T_34182F31.main);
                    T_34182F31.main.transform.position = __instance.transform.position;
                    T_34182F31.main.transform.up = Vector3.up;
                    T_34182F31.main.transform.forward = rotation * __instance.transform.forward;
                }
                else if (__instance.specificTween == -1)
                {
                    __instance.m_camera.ApplyValues(T_34182F31.main);
                    T_34182F31.main.transform.position = __instance.transform.position;
                    T_34182F31.main.transform.up = Vector3.up;
                    T_34182F31.main.transform.forward = __instance.m_exitTweenGoalOrientation * __instance.transform.forward;
                }
                else
                {
                    __instance.m_cameraStartTransitionTime = 0f;
                    __instance.m_cameraStartMaxTransitionSpeed = 500f;
                    __instance.m_cameraUseDefaultStartTransition = false;
                    T_34182F31.main.transform.position = __instance.transform.position;
                    T_34182F31.main.transform.rotation = __instance.transform.rotation;
                }
            }
            else if (T_A6E913D1.Instance.m_gameModeManager.CurrentMode == eGameMode.kFreeRoam || __instance.m_isCustomizationFreeLook)
            {
                T_34182F31.main.nearClipPlane = 0.01f;
                T_34182F31.main.transform.position = __instance.transform.position;
                T_34182F31.main.transform.rotation = __instance.transform.rotation;
            }
            return false;
        }

        public static void SetCameraPosition(Camera _13A97A3A2, Vector3 _1ACF98885)
        {
            if (T_A6E913D1.Instance.m_gameModeManager.CurrentMode != eGameMode.kFreeRoam)
            {
                VRRig.Instance.transform.position = _1ACF98885 - Vector3.up;
                Vector3 rot = _13A97A3A2.transform.eulerAngles;
                rot.x = 0;
                rot.z = 0;
                VRRig.Instance.transform.eulerAngles = rot;
            }
        }

        public static bool UpdateUIFade(float _13C05413A, Color _1A2D6C82C)
        {
            if (T_A6E913D1.Instance != null && T_A6E913D1.Instance.m_overrideBlackScreen)
                SteamVR_Fade.Start(Color.black, 0);
            else
            {
                _1A2D6C82C.a = _13C05413A;
                SteamVR_Fade.Start(_1A2D6C82C, 0);
            }
            
            return false;
        }

        public static bool MirrorReflectionAwake(T_55EA835B __instance)
        {
            __instance.enabled = false;
            __instance.GetComponent<MeshRenderer>().sharedMaterial.shader = Resources.MirrorShader;
            VRMirrorReflection reflection = __instance.gameObject.AddComponent<VRMirrorReflection>();
            reflection.m_DisablePixelLights = __instance.m_DisablePixelLights;
            reflection.m_TextureSize = __instance.m_TextureSize;
            reflection.m_ClipPlaneOffset = __instance.m_ClipPlaneOffset;
            reflection.m_ReflectLayers = __instance.m_ReflectLayers;
            return false;
        }

        private static readonly string[] scrollingTextOptions =
        {
            "Join the Flat2VR Discord (https://flat2vr.com) for more Flatscreen To VR mods!",
            "Support me on Ko-fi! https://ko-fi.com/trevtv",
            "I hope Deck Nine approves of this mod...",
            "Thank you for trying my VR mod!"
        };

        public static bool ReplaceScrollingText(ref string __result)
        {
            __result = scrollingTextOptions[UnityEngine.Random.Range(0, scrollingTextOptions.Length)];
            return false;
        }

        public static bool BoundaryStart(T_3BE79CFB __instance)
        {
            __instance.GetComponent<Collider>().isTrigger = false;
            return false;
        }

        public static void OnPPEnable(T_190FC323 __instance)
        {
            if (VRRig.Instance.Camera.GetComponent<VRPostProcessing>())
                return;

            __instance.enabled = false;
            var vpp = VRRig.Instance.Camera.gameObject.AddComponent<VRPostProcessing>();
            vpp.profile = __instance.profile;
        }

        #endregion

        #region NoVR Patches

        public static void InitNoVR(HarmonyLib.Harmony hInstance)
        {
            HarmonyInstance = hInstance;
            // Debug Stuff
            PatchPre(typeof(T_A6E913D1).GetMethod("IsAllowDebugOptions"), nameof(ReturnTrue));
            PatchPre(typeof(T_A6E913D1).GetMethod("IsTool"), nameof(ReturnTrue));
            PatchPost(typeof(T_EDB11480).GetMethod("StartSplash"), nameof(DisableSplashScreen));
            PatchPre(typeof(T_BF5A5EEC).GetMethod("SkipPressed"), nameof(CutsceneSkipPressed));
            PatchPost(typeof(T_6B664603).GetMethod("SetMode"), nameof(OnSetMode2));
            PatchPre(typeof(T_421B9CDF).GetMethod("SetCameraPosition"), nameof(SetCameraPosition2));
        }

        private static bool wasLastDistZero;

        public static void SetCameraPosition2(Camera _13A97A3A2, Vector3 _1ACF98885)
        {
            if (T_A6E913D1.Instance.m_gameModeManager.CurrentMode != eGameMode.kFreeRoam)
            {
                float f = Vector3.Distance(_13A97A3A2.transform.position, _1ACF98885);
                if (f != 0)
                {
                    if (wasLastDistZero)
                        MelonLogger.Msg("finished 0s");
                    MelonLogger.Msg("Distance between Camera positions: " + f.ToString());
                }
                else if (!wasLastDistZero)
                {
                    MelonLogger.Msg("Distance between Camera positions: 0");
                    wasLastDistZero = true;
                }
            }
        }

        public static void OnPPEnable2(T_190FC323 __instance)
        {
            if (__instance.GetComponent<DawnVR.Modules.VR.VRPostProcessing>())
                return;

            __instance.enabled = false;
            var vpp = __instance.gameObject.AddComponent<DawnVR.Modules.VR.VRPostProcessing>();
            vpp.profile = __instance.profile;
        }

        public static void OnSetMode2(bool __result, eGameMode _1C57B7248)
        {
            if (__result)
            {
                MelonLogger.Msg("Game successfully updated to mode " + _1C57B7248);
            }
        }

        public static bool ReturnTrue(ref bool __result)
        {
            __result = true;
            return false;
        }

        public static bool DontRunMe()
        {
            return false;
        }

        #endregion
    }
}
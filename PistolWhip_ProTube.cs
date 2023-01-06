﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

using MelonLoader;
using HarmonyLib;
using Il2Cpp;

[assembly: MelonInfo(typeof(PistolWhip_ProTube.PistolWhip_ProTube), "PistolWhip_ProTube", "1.0.0", "Florian Fahrenberger")]
[assembly: MelonGame("Cloudhead Games, Ltd.", "Pistol Whip")]



namespace PistolWhip_ProTube
{
    public class PistolWhip_ProTube : MelonMod
    {
        public static bool rightGunHasAmmo = true;
        public static bool leftGunHasAmmo = true;
        public static bool reloadHip = true;
        public static bool reloadShoulder = false;
        public static bool reloadTrigger = false;
        public static bool justKilled = false;

        public override void OnInitializeMelon()
        {
            ForceTubeVRInterface.InitAsync();
        }

        private static void setAmmo(bool hasAmmo, bool isRight)
        {
            if (isRight) { rightGunHasAmmo = hasAmmo; }
            else { leftGunHasAmmo = hasAmmo; }
        }


        private static bool checkIfRightHand(string controllerName)
        {
            if (controllerName.Contains("Right") | controllerName.Contains("right"))
            {
                return true;
            }
            else { return false; }
        }

        
        [HarmonyPatch(typeof(MeleeWeapon), "ProcessHit")]
        public class bhaptics_MeleeHit
        {
            [HarmonyPostfix]
            public static void Postfix(MeleeWeapon __instance)
            {
                //bool isRightHand = false;
                //if (checkIfRightHand(__instance.hand.name)) isRightHand = true;
                ForceTubeVRInterface.Rumble(50, 100.0f);
            }
        }


        [HarmonyPatch(typeof(Gun), "Fire")]
        public class bhaptics_GunFired
        {
            [HarmonyPostfix]
            public static void Postfix(Gun __instance)
            {
                //bool isRightHand;
                if (checkIfRightHand(__instance.hand.name))
                {
                    //isRightHand = true;
                    if (!rightGunHasAmmo) { return; }
                }
                else
                {
                    //isRightHand = false;
                    if (!leftGunHasAmmo) { return; }
                }
                byte kickPower = 150;
                switch (__instance.gunType)
                {
                    case 0:
                        // Pistol
                        kickPower = 150;
                        break;
                    case 1:
                        // Revolver
                        kickPower = 200;
                        break;
                    case 2:
                        // Burstfire
                        kickPower = 120;
                        break;
                    case 3:
                        // Boomstick (Shotgun)
                        kickPower = 250;
                        break;
                    case 4:
                        // Knuckles
                        return;
                        //kickPower = 50;
                        //break;
                    default:
                        kickPower = 150;
                        break;
                }
                ForceTubeVRInterface.Kick(kickPower, ForceTubeVRChannel.all);
            }
        }

        [HarmonyPatch(typeof(Reloader), "SetReloadMethod")]
        public class bhaptics_ReloadMethod
        {
            [HarmonyPostfix]
            public static void Postfix(Reloader __instance, Reloader.ReloadMethod method)
            {
                try
                {
                    if (method.ToString() == "Gesture") { reloadTrigger = false; }
                    else { reloadTrigger = true; }
                }
                catch { return; }
            }
        }


        [HarmonyPatch(typeof(Gun), "Reload")]
        public class bhaptics_GunReload
        {
            [HarmonyPostfix]
            public static void Postfix(Gun __instance, bool triggeredByMelee)
            {
                try
                {
                    if (!__instance.reloadTriggered) { return; }
                    if (triggeredByMelee) { return; }
                }
                catch { return; }
                if (__instance.reloadGestureTypeVar.Value == ESettings_ReloadType.DOWN) { reloadHip = true; reloadShoulder = false; }
                if (__instance.reloadGestureTypeVar.Value == ESettings_ReloadType.UP) { reloadHip = false; reloadShoulder = true; }
                if (__instance.reloadGestureTypeVar.Value == ESettings_ReloadType.BOTH)
                {
                    if ((__instance.player.head.position.y - __instance.hand.position.y) >= 0.3f) { reloadHip = true; reloadShoulder = false; }
                    else { reloadHip = false; reloadShoulder = true; }
                }
                //if (__instance.nextReload >= 5.0f) { return; }
                //bool isRightHand;
                //if (checkIfRightHand(__instance.hand.name)) { isRightHand = true; }
                //else { isRightHand = false; }
                //tactsuitVr.GunReload(isRightHand, reloadHip, reloadShoulder, reloadTrigger);
                ForceTubeVRInterface.Rumble(20, 100f);
            }
        }


        [HarmonyPatch(typeof(GunAmmoDisplay), "Update")]
        public class bhaptics_GunHasAmmo
        {
            [HarmonyPostfix]
            public static void Postfix(GunAmmoDisplay __instance)
            {
                bool isRightHand;
                bool hasAmmo = true;
                string handName = "";
                int numberBullets = 0;
                try { handName = __instance.gun.hand.name; numberBullets = __instance.currentBulletCount; }
                catch { return; }
                if (checkIfRightHand(handName)) { isRightHand = true; }
                else { isRightHand = false; }
                if (numberBullets == 0) { hasAmmo = false; }
                setAmmo(hasAmmo, isRightHand);
            }
        }



    }
}

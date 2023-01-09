using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using UnityEngine;

using MelonLoader;
using HarmonyLib;
using Il2Cpp;

[assembly: MelonInfo(typeof(PistolWhip_ProTube.PistolWhip_ProTube), "PistolWhip_ProTube", "1.1.1", "Florian Fahrenberger")]
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
        public static string configPath = Directory.GetCurrentDirectory() + "\\UserData\\";
        public static bool dualWield = false;

        public override void OnInitializeMelon()
        {
            InitializeProTube();
        }

        public static void saveChannel(string channelName, string proTubeName)
        {
            string fileName = configPath + channelName + ".pro";
            File.WriteAllText(fileName, proTubeName, Encoding.UTF8);
        }

        public static string readChannel(string channelName)
        {
            string fileName = configPath + channelName + ".pro";
            if (!File.Exists(fileName)) return "";
            return File.ReadAllText(fileName, Encoding.UTF8);
        }

        public static void dualWieldSort()
        {
            //MelonLogger.Msg("Channels: " + ForceTubeVRInterface.ListChannels());
            JsonDocument doc = JsonDocument.Parse(ForceTubeVRInterface.ListChannels());
            JsonElement pistol1 = doc.RootElement.GetProperty("channels").GetProperty("pistol1");
            JsonElement pistol2 = doc.RootElement.GetProperty("channels").GetProperty("pistol2");
            if ((pistol1.GetArrayLength() > 0) && (pistol2.GetArrayLength() > 0))
            {
                dualWield = true;
                MelonLogger.Msg("Two ProTube devices detected, player is dual wielding.");
                if ((readChannel("pistol1") == "") || (readChannel("pistol2") == ""))
                {
                    MelonLogger.Msg("No configuration files found, saving current right and left hand pistols.");
                    saveChannel("pistol1", pistol1[0].GetProperty("name").ToString());
                    saveChannel("pistol2", pistol2[0].GetProperty("name").ToString());
                }
                else
                {
                    string rightHand = readChannel("pistol1");
                    string leftHand = readChannel("pistol2");
                    MelonLogger.Msg("Found and loaded configuration. Right hand: " + rightHand + ", Left hand: " + leftHand);
                    ForceTubeVRInterface.ClearChannel(4);
                    ForceTubeVRInterface.ClearChannel(5);
                    ForceTubeVRInterface.AddToChannel(4, rightHand);
                    ForceTubeVRInterface.AddToChannel(5, leftHand);
                }
            }
        }

        private async void InitializeProTube()
        {
            MelonLogger.Msg("Initializing ProTube gear...");
            await ForceTubeVRInterface.InitAsync(true);
            Thread.Sleep(10000);
            dualWieldSort();
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
                ForceTubeVRChannel myChannel = ForceTubeVRChannel.pistol1;
                if (!checkIfRightHand(__instance.hand.name))
                    myChannel = ForceTubeVRChannel.pistol2;
                ForceTubeVRInterface.Rumble(255, 200.0f, myChannel);
            }
        }


        [HarmonyPatch(typeof(Gun), "Fire")]
        public class bhaptics_GunFired
        {
            [HarmonyPostfix]
            public static void Postfix(Gun __instance)
            {
                ForceTubeVRChannel myChannel = ForceTubeVRChannel.pistol1;
                if (checkIfRightHand(__instance.hand.name))
                {
                    if (!rightGunHasAmmo) { return; }
                }
                else
                {
                    myChannel = ForceTubeVRChannel.pistol2;
                    if (!leftGunHasAmmo) { return; }
                }
                byte kickPower = 210;
                switch (__instance.gunType)
                {
                    case 0:
                        // Pistol
                        kickPower = 210;
                        break;
                    case 1:
                        // Revolver
                        kickPower = 230;
                        break;
                    case 2:
                        // Burstfire
                        kickPower = 180;
                        break;
                    case 3:
                        // Boomstick (Shotgun)
                        ForceTubeVRInterface.Shoot(255, 200, 100f, myChannel);
                        return;
                    case 4:
                        // Knuckles
                        ForceTubeVRInterface.Rumble(255, 200f, myChannel);
                        return;
                    default:
                        kickPower = 210;
                        break;
                }
                ForceTubeVRInterface.Kick(kickPower, myChannel);
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
                ForceTubeVRChannel myChannel = ForceTubeVRChannel.pistol1;
                if (!checkIfRightHand(__instance.hand.name))
                    myChannel = ForceTubeVRChannel.pistol2;
                ForceTubeVRInterface.Rumble(200, 100f, myChannel);
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

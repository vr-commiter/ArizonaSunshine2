using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using MyTrueGear;
using System.Threading;
using Vertigo;
using Vertigo.AZS2.Client;
using Vertigo.AZS2.Melee;
using Vertigo.AZS2;
using Vertigo.Haptics;
using Vertigo.Interactables;
using Vertigo.VertigoInput;
using Vertigo.VR;
using System.Collections.Generic;
using UnityEngine;
using System;
using Il2CppVertigo.AZS2.Client;

namespace ArizonaSunshine2_TrueGear
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BasePlugin
    {
        public static new ManualLogSource Log;

        private static TrueGearMod _TrueGear = null;

        private static bool canChestSlot = true;
        private static bool canLeftShoot = true;
        private static bool canRightShoot = true;

        private static IInteractableSlot gloveSlot = null;
        private static bool isRightHandRemoveItem = false;
        private static bool isLeftHandRemoveItem = false;
        private static string leftGloveItem = "";
        private static string rightGloveItem = "";

        private static int chestSleepTime = 30;
        private static int shootLeftSleepTime = 80;
        private static int shootRightSleepTime = 80;

        private static float playerHealth = 0;
        private static float maxPlayerHealth = 0;

        private static bool canFlamethrower = true;

        private static string flameThrowerShootName = "";
        private static Timer flameThrowerShoot = null;

        public override void Load()
        {
            // Plugin startup logic
            Log = base.Log;

            HarmonyLib.Harmony.CreateAndPatchAll(typeof(Plugin));

            _TrueGear = new TrueGearMod();
            _TrueGear.Play("HeartBeat");


            new Thread(new ThreadStart(this.ChestSlotTimerCallBack)).Start();
            new Thread(new ThreadStart(this.LeftShootTimerCallBack)).Start();
            new Thread(new ThreadStart(this.RightShootTimerCallBack)).Start();

            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }

        public static KeyValuePair<float, float> GetAngle(Vector3 playerPosition, Vector3 hitPosition, Quaternion playerRotation)
        {
            //Log.LogInfo("-----------------------------------------------");
            //Log.LogInfo($"playerPosition | X : {playerPosition.x},Y : {playerPosition.y},Z : {playerPosition.z}");
            //Log.LogInfo($"hitPosition | X : {hitPosition.x},Y : {hitPosition.y},Z : {hitPosition.z}");
            Vector3 directionToHit = hitPosition - playerPosition;
            Vector3 relativeDirection = Quaternion.Inverse(playerRotation) * directionToHit;
            float angle = Mathf.Atan2(relativeDirection.x, relativeDirection.z) * Mathf.Rad2Deg;
            angle = (360f - angle) % 360f;
            //Log.LogInfo($"angle :{angle}");
            // 返回角度，其中正值表示右侧，负值表示左侧
            float verticalDifference = hitPosition.y - playerPosition.y;
            return new KeyValuePair<float, float>(angle, verticalDifference);
        }


        [HarmonyPostfix, HarmonyPatch(typeof(Vertigo.AZS2.Client.PlayerHittableModule), "HandleOnHitEvent")]
        public static void PlayerHittableModulee_HandleOnHitEvent_Prefix(Vertigo.AZS2.Client.PlayerHittableModule __instance, HitArgs args)
        {
            if (!__instance.healthModule.identityModule.IsLocal)
            {
                return;
            }
            Log.LogInfo("-----------------------------------------------");
            //Log.LogInfo("HandleOnHitEvent");
            //Log.LogInfo($"WorldHitPosition | X : {args.WorldHitNormal.x},Y : {args.WorldHitNormal.y},Z : {args.WorldHitNormal.z}");
            //Log.LogInfo($"WorldHitPosition | X : {args.WorldHitDirection.x},Y : {args.WorldHitDirection.y},Z : {args.WorldHitDirection.z}");
            //Log.LogInfo($"WorldHitPosition | X : {args.WorldHitPosition.x},Y : {args.WorldHitPosition.y},Z : {args.WorldHitPosition.z}");
            //Log.LogInfo($"HeadPosition | X : {__instance.PlayerHittable.transformModule.HeadPosition.x},Y : {__instance.PlayerHittable.transformModule.HeadPosition.y},Z : {__instance.PlayerHittable.transformModule.HeadPosition.z}");
            var playerPos = __instance.PlayerHittable.transformModule.HeadPosition;
            var hitPos = args.WorldHitPosition - args.WorldHitDirection;
            var playerRotation = __instance.PlayerHittable.transformModule.ChestRotation;
            //Log.LogInfo($"hitPos | X : {hitPos.x},Y : {hitPos.y},Z : {hitPos.z}");
            var angle = GetAngle(playerPos, hitPos, playerRotation);
            Log.LogInfo($"DefaultDamage,{angle.Key},{angle.Value}");
            _TrueGear.PlayAngle("DefaultDamage", angle.Key,0);

            playerHealth = __instance.healthModule.HealthValue;
            maxPlayerHealth = __instance.healthModule.MaxHealth;
            Log.LogInfo("-----------------------------------------------");
            Log.LogInfo("playerHealth :" + playerHealth);
            if (playerHealth > 0f)
            {
                //var headPoint = __instance.transformModule.HeadPosition;
                //var hitPoint = hitOrigin.Value;
                //var playerRotation = __instance.transformModule.ChestRotation;
                //var angle = GetAngle(headPoint, hitPoint, playerRotation);
                //Log.LogInfo("-----------------------------------------------");
                //Log.LogInfo($"DefaultDamage,{angle.Key},{angle.Value + 1.5f}");
                //_TrueGear.PlayAngle("DefaultDamage",angle.Key,angle.Value + 1.5f);

                if (playerHealth < maxPlayerHealth * 0.25f)
                {
                    Log.LogInfo("-----------------------------------------------");
                    Log.LogInfo("StartHeartBeat");
                    _TrueGear.StartHeartBeat();
                }
                else
                {
                    Log.LogInfo("-----------------------------------------------");
                    Log.LogInfo("StopHeartBeat");
                    _TrueGear.StopHeartBeat();
                }
            }
            else
            {
                Log.LogInfo("-----------------------------------------------");
                Log.LogInfo("PlayerDeath");
                _TrueGear.Play("PlayerDeath");
                Log.LogInfo("StopHeartBeat");
                _TrueGear.StopHeartBeat();
                return;
            }
        }

        //[HarmonyPostfix, HarmonyPatch(typeof(Vertigo.AZS2.Client.ClientPlayerHealthModule), "RequestDamage")]
        //public static void ClientPlayerHealthModule_RequestDamage_Postfix(Vertigo.AZS2.Client.ClientPlayerHealthModule __instance)
        //{
        //    if (!__instance.identityModule.IsLocal)
        //    {
        //        return;
        //    }

        //    playerHealth = __instance.HealthValue;
        //    maxPlayerHealth = __instance.MaxHealth;
        //    Log.LogInfo("-----------------------------------------------");
        //    Log.LogInfo("playerHealth :" + playerHealth);
        //    if (__instance.HealthValue > 0f)
        //    {
        //        //var headPoint = __instance.transformModule.HeadPosition;
        //        //var hitPoint = hitOrigin.Value;
        //        //var playerRotation = __instance.transformModule.ChestRotation;
        //        //var angle = GetAngle(headPoint, hitPoint, playerRotation);
        //        //Log.LogInfo("-----------------------------------------------");
        //        //Log.LogInfo($"DefaultDamage,{angle.Key},{angle.Value + 1.5f}");
        //        //_TrueGear.PlayAngle("DefaultDamage",angle.Key,angle.Value + 1.5f);

        //        if (__instance.HealthValue < __instance.MaxHealth * 0.25f)
        //        {
        //            Log.LogInfo("-----------------------------------------------");
        //            Log.LogInfo("StartHeartBeat");
        //            _TrueGear.StartHeartBeat();
        //        }
        //        else
        //        {
        //            Log.LogInfo("-----------------------------------------------");
        //            Log.LogInfo("StopHeartBeat");
        //            _TrueGear.StopHeartBeat();
        //        }
        //    }
        //    else
        //    {
        //        Log.LogInfo("-----------------------------------------------");
        //        Log.LogInfo("PlayerDeath");
        //        _TrueGear.Play("PlayerDeath");
        //        Log.LogInfo("StopHeartBeat");
        //        _TrueGear.StopHeartBeat();
        //        return;
        //    }

        //    //if (isKilled)
        //    //{
        //    //    Log.LogInfo("-----------------------------------------------");
        //    //    Log.LogInfo("PlayerDeath");
        //    //    _TrueGear.Play("PlayerDeath");
        //    //    Log.LogInfo("StopHeartBeat");
        //    //    _TrueGear.StopHeartBeat();
        //    //}
        //}



        [HarmonyPostfix, HarmonyPatch(typeof(Vertigo.AZS2.Client.ProjectileShootStrategyBehaviourData), "PlayShootHapticsForHand")]
        public static void ProjectileShootStrategyBehaviourData_PlayShootHapticsForHand_Postfix(Vertigo.AZS2.Client.ProjectileShootStrategyBehaviourData __instance, Vertigo.AZS2.Client.AZS2Hand hand)
        {

            if (__instance.shootStrategy.item.IsGrabbedLocally)
            {
                string weaponType = "Pistol";
                if (__instance.shootStrategy.hasSpreadPattern)
                    if (__instance.shootStrategy.hasSpreadPattern)
                    {
                        weaponType = "Shotgun";
                    }
                if (__instance.shootStrategy.firingMode == EFiringMode.FullAuto)
                {
                    weaponType = "Rifle";
                }
                bool isLeftHand = hand.IsLeftHand;

                if (isLeftHand)
                {
                    if (!canLeftShoot)
                    {
                        return;
                    }
                    canLeftShoot = false;
                    Log.LogInfo("-----------------------------------------------");
                    Log.LogInfo($"LeftHand{weaponType}Shoot");
                    _TrueGear.Play("LeftHand" + weaponType + "Shoot");
                }
                else
                {
                    if (!canRightShoot)
                    {
                        return;
                    }
                    canRightShoot = false;
                    Log.LogInfo("-----------------------------------------------");
                    Log.LogInfo($"RightHand{weaponType}Shoot");
                    _TrueGear.Play("RightHand" + weaponType + "Shoot");
                }
            }
        }

        private void LeftShootTimerCallBack()
        {
            while (true)
            {
                canLeftShoot = true;
                Thread.Sleep(shootLeftSleepTime);
            }
        }

        private void RightShootTimerCallBack()
        {
            while (true)
            {
                canRightShoot = true;
                Thread.Sleep(shootRightSleepTime);
            }
        }



        [HarmonyPostfix, HarmonyPatch(typeof(Vertigo.AZS2.Client.ClientExplosiveItemFeature), "Explode")]
        public static void ClientExplosiveItemFeature_Explode_Postfix(Vertigo.AZS2.Client.ClientExplosiveItemFeature __instance)
        {
            Log.LogInfo("-----------------------------------------------");
            Log.LogInfo("Explosion");
            _TrueGear.Play("Explosion");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Vertigo.AZS2.Client.HolsterHandleSlotBehaviour), "OnInteractableInserted")]
        public static void HolsterHandleSlotBehaviour_OnInteractableInserted_Postfix(Vertigo.AZS2.Client.HolsterHandleSlotBehaviour __instance, InteractableHandle handle)
        {
            if (Vertigo.AZS2.Client.PawnUtils.IsLocalPawnSlot(__instance.slot))
            {
                Log.LogInfo("-----------------------------------------------");
                Log.LogInfo($"{__instance.slotType.ToString()}HipSlotInputItem");
                _TrueGear.Play(__instance.slotType.ToString() + "HipSlotInputItem");
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Vertigo.AZS2.Client.HolsterHandleSlotBehaviour), "HandleOnInteractableRemovedEvent")]
        public static void HolsterHandleSlotBehaviour_HandleOnInteractableRemovedEvent_Postfix(Vertigo.AZS2.Client.HolsterHandleSlotBehaviour __instance, InteractableHandle handle)
        {
            if (Vertigo.AZS2.Client.PawnUtils.IsLocalPawnSlot(handle.Slot))
            {
                Log.LogInfo("-----------------------------------------------");
                Log.LogInfo($"{__instance.slotType.ToString()}HipSlotOutputItem");
                _TrueGear.Play(__instance.slotType.ToString() + "HipSlotOutputItem");
            }
        }


        [HarmonyPostfix, HarmonyPatch(typeof(Vertigo.AZS2.Client.GrabReleaseInteractableHandleSlotBehaviour), "SetSlot")]
        public static void GrabReleaseInteractableHandleSlotBehaviour_SetSlot_Postfix(IInteractableSlot slot)
        {
            gloveSlot = slot;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Vertigo.AZS2.Client.GrabReleaseInteractableHandleSlotBehaviour), "OnInteractableInserted")]
        public static void GrabReleaseInteractableHandleSlotBehaviour_OnInteractableInserted_Postfix(Vertigo.AZS2.Client.GrabReleaseInteractableHandleSlotBehaviour __instance, InteractableHandle handle)
        {
            //if (PawnUtils.IsLocalPawnSlot(gloveSlot))
            //{
            if (isLeftHandRemoveItem)
            {
                Log.LogInfo("-----------------------------------------------");
                Log.LogInfo("RightGloveSlotInputItem");
                _TrueGear.Play("RightGloveSlotInputItem");
                rightGloveItem = __instance.currentInsertedItemBehaviour.name;
            }
            if (isRightHandRemoveItem)
            {
                Log.LogInfo("-----------------------------------------------");
                Log.LogInfo("LeftGloveSlotInputItem");
                _TrueGear.Play("LeftGloveSlotInputItem");
                leftGloveItem = __instance.currentInsertedItemBehaviour.name;
            }
            //}
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Vertigo.AZS2.Client.GrabReleaseInteractableHandleSlotBehaviour), "HandleOnInteractableRemovedEvent")]
        public static void GrabReleaseInteractableHandleSlotBehaviour_HandleOnInteractableRemovedEvent_Postfix(Vertigo.AZS2.Client.GrabReleaseInteractableHandleSlotBehaviour __instance, InteractableHandle handle)
        {
            //if (PawnUtils.IsLocalPawnSlot(handle.Slot))
            //{
            if (__instance.previousInsertedItemBehaviour.name == leftGloveItem && leftGloveItem != "")
            {
                Log.LogInfo("-----------------------------------------------");
                Log.LogInfo("LeftGloveSlotOutputItem");
                _TrueGear.Play("LeftGloveSlotOutputItem");
                leftGloveItem = "";
            }
            if (__instance.previousInsertedItemBehaviour.name == rightGloveItem && rightGloveItem != "")
            {
                Log.LogInfo("-----------------------------------------------");
                Log.LogInfo("RightGloveSlotOutputItem");
                _TrueGear.Play("RightGloveSlotOutputItem");
                rightGloveItem = "";
            }
            //}
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Vertigo.AZS2.Client.AmmoPouchResourceViewBehaviour), "HandleOnResourceValueChanged")]
        public static void AmmoPouchResourceViewBehaviour_HandleOnResourceValueChanged_Postfix(Vertigo.AZS2.Client.AmmoPouchResourceViewBehaviour __instance, uint resourceId, uint oldValue, uint newValue)
        {
            if (!canChestSlot)
            {
                return;
            }
            canChestSlot = false;
            if (oldValue < newValue)
            {
                Log.LogInfo("-----------------------------------------------");
                Log.LogInfo("ChestSlotInputItem");
                _TrueGear.Play("ChestSlotInputItem");
            }
            else if (newValue < oldValue)
            {
                Log.LogInfo("-----------------------------------------------");
                Log.LogInfo("ChestSlotOutputItem");
                _TrueGear.Play("ChestSlotOutputItem");
            }
        }

        private void ChestSlotTimerCallBack()
        {
            while (true)
            {
                Thread.Sleep(chestSleepTime);
                canChestSlot = true;
            }

        }
        /*
        [HarmonyPrefix, HarmonyPatch(typeof(DogPetHandleBehaviour), "SetFullyAttached")]
        public static void DogPetHandleBehaviour_SetFullyAttached_Prefix(DogPetHandleBehaviour __instance, AZS2Hand hand, bool isFullyAttached)
        {

            if (PawnUtils.IsLocalHand(PawnUtils.GetSlotForHand(PawnUtils.GetPawnForHand(hand), hand)))
            {
                if (isFullyAttached)
                {
                    isStroke = true;
                    if (hand.IsLeftHand)
                    {
                        Log.LogInfo("-----------------------------------------------");
                        Log.LogInfo("LeftHandStrokeBuddy");
                        _TrueGear.StartLeftHandStrokeBuddy();
                    }
                    else
                    {
                        Log.LogInfo("-----------------------------------------------");
                        Log.LogInfo("RightHandStrokeBuddy");
                        _TrueGear.StartRightHandStrokeBuddy();
                    }
                }
            }
        }

        */

        /*
        [HarmonyPostfix, HarmonyPatch(typeof(AmmoItemFeatureBehaviourData), "HandleOnGrabbedEvent")]
        public static void AmmoItemFeatureBehaviourData_HandleOnGrabbedEvent_Postfix(AmmoItemFeatureBehaviourData __instance,ClientItem item, Entity pawn, Hand hand)
        {
            if (PawnUtils.IsLocalPawnSlot(PawnUtils.GetSlotForHand(pawn, hand)))
            {
                if (hand.IsLeftHand)
                {
                    Log.LogInfo("-----------------------------------------------");
                    Log.LogInfo("LeftHandPickupItem");
                    _TrueGear.Play("LeftHandPickupItem");
                }
                else 
                {
                    Log.LogInfo("-----------------------------------------------");
                    Log.LogInfo("RightHandPickupItem");
                    _TrueGear.Play("RightHandPickupItem");
                }
            }
        }
        */
        [HarmonyPostfix, HarmonyPatch(typeof(Il2CppVertigo.AZS2.Client.MuzzleSlideSprintChecker), "OnRightHandSlotInteractableRemoved")]
        public static void MuzzleSlideSprintChecker_OnRightHandSlotInteractableRemoved_Postfix(Il2CppVertigo.AZS2.Client.MuzzleSlideSprintChecker __instance)
        {
            if (!isRightHandRemoveItem)
            {
                isRightHandRemoveItem = true;
                Timer rightHandRemoveItemTimer = new Timer(RightHandRemoveItemTimerCallBack, null, 5, Timeout.Infinite);
            }
        }
        private static void RightHandRemoveItemTimerCallBack(object o)
        {
            isRightHandRemoveItem = false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Il2CppVertigo.AZS2.Client.MuzzleSlideSprintChecker), "OnLeftHandSlotInteractableRemoved")]
        public static void MuzzleSlideSprintChecker_OnLeftHandSlotInteractableRemoved_Postfix(Il2CppVertigo.AZS2.Client.MuzzleSlideSprintChecker __instance)
        {
            if (!isLeftHandRemoveItem)
            {
                isLeftHandRemoveItem = true;
                Timer leftHandRemoveItemTimer = new Timer(LeftHandRemoveItemTimerCallBack, null, 5, Timeout.Infinite);
            }
        }
        private static void LeftHandRemoveItemTimerCallBack(object o)
        {
            isLeftHandRemoveItem = false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StabStrategy), "DoHit")]
        public static void StabStrategy_DoHit_Postfix(StabStrategy __instance, HittableBehaviour hittableBehaviour)
        {
            if (Vertigo.AZS2.Client.PawnUtils.IsLocalHand(Vertigo.AZS2.Client.PawnUtils.GetSlotForHand(__instance.playerSystem.LocalPawn, __instance.equippedHand)))
            {
                if (__instance.equippedHand.HandSide == EHandSide.Left)
                {
                    Log.LogInfo("-----------------------------------------------");
                    Log.LogInfo("LeftHandMeleeHit");
                    _TrueGear.Play("LeftHandMeleeHit");
                }
                else
                {
                    Log.LogInfo("-----------------------------------------------");
                    Log.LogInfo("RightHandMeleeHit");
                    _TrueGear.Play("RightHandMeleeHit");
                }
            }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(DeviceHapticsSystem), "PlayHaptics", new System.Type[] { typeof(EControllerRole), typeof(uint), typeof(bool) })]
        private static void DeviceHapticsSystem_PlayHaptics1_Postfix(DeviceHapticsSystem __instance, EControllerRole controllerRoleMask, uint hapticsProfileId, bool loopHaptics = false)
        {
            canFlamethrower = false;
            Timer flamethrowerTimer = new Timer(flamethrowerTimerCallBack, null, 10, Timeout.Infinite);
            //Log.LogInfo(hapticsProfileId);

            if (flameThrowerShoot != null)
            {
                flameThrowerShootName = "";
                flameThrowerShoot.Dispose();
                flameThrowerShoot = null;
            }

            if (hapticsProfileId != 5)
            {
                return;
            }

            if ((controllerRoleMask & EControllerRole.Left) == EControllerRole.Left)
            {
                Log.LogInfo("-----------------------------------------------");
                Log.LogInfo("LeftHandPickupItem");
                _TrueGear.Play("LeftHandPickupItem");
            }
            else if ((controllerRoleMask & EControllerRole.Right) == EControllerRole.Right)
            {
                Log.LogInfo("-----------------------------------------------");
                Log.LogInfo("RightHandPickupItem");
                _TrueGear.Play("RightHandPickupItem");
            }
            Log.LogInfo(controllerRoleMask);
            Log.LogInfo(hapticsProfileId);
        }

        private static void flamethrowerTimerCallBack(object o)
        {
            canFlamethrower = true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(DeviceHapticsSystem), "PlayHaptics", new System.Type[] { typeof(EControllerRole), typeof(IDeviceHaptics), typeof(bool) })]
        private static void DeviceHapticsSystem_PlayHaptics2_Postfix(DeviceHapticsSystem __instance, EControllerRole controllerRoleMask, IDeviceHaptics haptics, bool loopHaptics = false)
        {
            if (!canFlamethrower)
            {
                return;
            }
            if (controllerRoleMask == EControllerRole.Left)
            {
                flameThrowerShootName = "LeftHandFlamethrower";
            }
            else if (controllerRoleMask == EControllerRole.Right)
            {
                flameThrowerShootName = "RightHandFlamethrower";
            }
            Log.LogInfo(controllerRoleMask);
            flameThrowerShoot = new Timer(FlameThrowerShootTimerCallBack, null, 10, Timeout.Infinite);
            //Log.LogInfo(haptics.ObjectClass);
            //Log.LogInfo(sss);
        }

        private static void FlameThrowerShootTimerCallBack(object o)
        {
            if (flameThrowerShootName == "")
            {
                return;
            }
            Log.LogInfo("-----------------------------------------------");
            Log.LogInfo(flameThrowerShootName);
            _TrueGear.Play(flameThrowerShootName);
        }


        /*
        
        [HarmonyPostfix, HarmonyPatch(typeof(DeviceHapticsSystem), "PlayHaptics", new System.Type[] { typeof(EControllerRole), typeof(float), typeof(float), typeof(float), typeof(bool) })]
        private static void DeviceHapticsSystem_PlayHaptics3_Postfix(DeviceHapticsSystem __instance, EControllerRole controllerRoleMask, float duration, float frequency, float intensity, bool loopHaptics = false)
        {
            Log.LogInfo("-----------------------------------------------");
            Log.LogInfo("LeftHandShake3");

            if (controllerRoleMask == EControllerRole.Left)
            {
                Log.LogInfo("-----------------------------------------------");
                Log.LogInfo("LeftHandShake3");
            }
            else
            {
                Log.LogInfo("-----------------------------------------------");
                Log.LogInfo("RightHandShake3");
            }
            Log.LogInfo(controllerRoleMask);
            Log.LogInfo(duration);
            Log.LogInfo(frequency);
            Log.LogInfo(intensity);
        }


        
        [HarmonyPrefix, HarmonyPatch(typeof(DeviceHapticsSystem), "UpdateHaptics", new System.Type[] { typeof(IHapticsReceiver), typeof(List<ControllerRoleHaptics.ActiveHaptics>), typeof(EHapticDeviceType),typeof(int) })]
        private static void DeviceHapticsSystem_UpdateHaptics_Postfix(IHapticsReceiver hapticsReceiver, List<ControllerRoleHaptics.ActiveHaptics> allActiveHaptics, EHapticDeviceType deviceType, int controllerType)
        {
            if (deviceType == EHapticDeviceType.Controller)
            {
                Log.LogInfo("-----------------------------------------------");
                Log.LogInfo("UpdateHaptics");
                Log.LogInfo(hapticsReceiver);
                Log.LogInfo(allActiveHaptics);
                Log.LogInfo(controllerType);
            }
        }
        
        [HarmonyPostfix, HarmonyPatch(typeof(DeviceHapticsSystem), "StopAllHaptics", new System.Type[] { typeof(EControllerRole) })]
        private static void DeviceHapticsSystem_StopHaptics_Postfix(DeviceHapticsSystem __instance, EControllerRole controllerRoleMask)
        {
            
                Log.LogInfo("-----------------------------------------------");
                Log.LogInfo("StopAllHaptics");
            Log.LogInfo(controllerRoleMask);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(DeviceHapticsSystem), "StopHaptics", new System.Type[] { typeof(EControllerRole), typeof(uint) })]
        private static void DeviceHapticsSystem_StopHaptics_Postfix(DeviceHapticsSystem __instance, EControllerRole controllerRoleMask, uint hapticsProfileId)
        {
            
                Log.LogInfo("-----------------------------------------------");
                Log.LogInfo("StopHaptics");
            Log.LogInfo(hapticsProfileId);
            Log.LogInfo(controllerRoleMask);

        }
        */


        [HarmonyPostfix, HarmonyPatch(typeof(DeviceHapticsSystem), "StopHaptics", new System.Type[] { typeof(EControllerRole), typeof(IDeviceHaptics) })]
        private static void DeviceHapticsSystem_StopHaptics2_Postfix(DeviceHapticsSystem __instance, EControllerRole controllerRoleMask, IDeviceHaptics haptic)
        {
            if ((controllerRoleMask & EControllerRole.Left) == EControllerRole.Left && (controllerRoleMask & EControllerRole.Right) == EControllerRole.Right && (controllerRoleMask & EControllerRole.SingleController) == EControllerRole.SingleController)
            {
                Log.LogInfo("-----------------------------------------------");
                Log.LogInfo("FallDamage");
                _TrueGear.Play("FallDamage");

            }
        }



        /*
        [HarmonyPostfix, HarmonyPatch(typeof(BuffItemDrinkStrategy), "StartDrinkingAudio")]
        private static void BuffItemDrinkStrategy_StartDrinkingAudio_Postfix(BuffItemDrinkStrategy __instance)
        {

            Log.LogInfo("-----------------------------------------------");
            Log.LogInfo("StartDrinkingAudio");
        }
        */

        [HarmonyPostfix, HarmonyPatch(typeof(Vertigo.AZS2.Client.ClientPlayerHealthModule), "ApplyHeal")]
        public static void ClientPlayerHealthModule_ApplyHeal_Postfix(Vertigo.AZS2.Client.ClientPlayerHealthModule __instance)
        {
            if (!__instance.identityModule.IsLocal)
            {
                return;
            }
            playerHealth = __instance.HealthValue;
            maxPlayerHealth = __instance.MaxHealth;
            if (__instance.HealthValue >= __instance.MaxHealth * 0.25f)
            {
                Log.LogInfo("-----------------------------------------------");
                Log.LogInfo("StopHeartBeat");
                _TrueGear.StopHeartBeat();
            }
            Log.LogInfo("-----------------------------------------------");
            Log.LogInfo("Healing");
            _TrueGear.Play("Healing");
        }


        [HarmonyPostfix, HarmonyPatch(typeof(Vertigo.AZS2.Client.ClientPlayerHealthModule), "RequestHeal")]
        public static void ClientPlayerHealthModule_RequestHeal_Postfix(Vertigo.AZS2.Client.ClientPlayerHealthModule __instance)
        {
            if (!__instance.identityModule.IsLocal)
            {
                return;
            }
            Log.LogInfo("-----------------------------------------------");
            Log.LogInfo("Eating");
            _TrueGear.Play("Eating");
        }

        [HarmonyPrefix, HarmonyPatch(typeof(Vertigo.AZS2.Client.MenuViewBehaviour), "OpenCloseMenu")]
        public static void MenuViewBehaviour_OpenCloseMenu_Prefix(Vertigo.AZS2.Client.MenuViewBehaviour __instance, bool isOpen)
        {
            if (isOpen)
            {
                Log.LogInfo("-----------------------------------------------");
                Log.LogInfo("StopHeartBeat");
                _TrueGear.StopHeartBeat();
            }
            else if (!isOpen && playerHealth < maxPlayerHealth * 0.25f)
            {
                Log.LogInfo("-----------------------------------------------");
                Log.LogInfo("StartHeartBeat");
                _TrueGear.StartHeartBeat();
            }
        }


        [HarmonyPrefix, HarmonyPatch(typeof(AZS2SceneManager), "SceneEnding")]
        public static void MenuViewBehaviour_SceneEnding_Prefix(AZS2SceneManager __instance)
        {
            playerHealth = 0;
            maxPlayerHealth = 0;
            Log.LogInfo("-----------------------------------------------");
            Log.LogInfo("StopHeartBeat");
            _TrueGear.StopHeartBeat();
        }
    }
}

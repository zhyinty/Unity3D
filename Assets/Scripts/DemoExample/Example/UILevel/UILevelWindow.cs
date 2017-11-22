﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace TinyFrameWork
{
    /// <summary>
    /// Level system window
    /// </summary>
    public class UILevelWindow : UIWindowBase, IWindowAnimation
    {
        public GameObject levelItem;
        private Transform trsLevelItemsParent;
        private TweenAlpha twAlpha;

        private bool realReturnToMainMenu = false;

        protected override void SetWindowId()
        {
            this.ID = WindowID.WindowID_Level;
        }

        public override void InitWindowOnAwake()
        {
            InitWindowCoreData();
            base.InitWindowOnAwake();
            trsLevelItemsParent = GameUtility.FindDeepChild(this.gameObject, "LevelItems/Items");
            twAlpha = gameObject.GetComponent<TweenAlpha>();
        }

        protected override void InitWindowCoreData()
        {
            base.InitWindowCoreData();
            this.preWindowID = WindowID.WindowID_MainMenu;

            this.windowData.showMode = UIWindowShowMode.HideOtherWindow;
            this.windowData.navigationMode = UIWindowNavigationMode.NormalNavigation;
            this.windowData.colliderMode = UIWindowColliderMode.Normal;
        }

        public override void ShowWindow(BaseWindowContextData levelContextData)
        {
            realReturnToMainMenu = false;
            ResetAnimation();
            IsLock = true;
            NGUITools.SetActive(this.gameObject, true);
            FillLevelItems();

            EnterAnimation(delegate
            {
                IsLock = false;
            });

            // When exit the window execute logic
            // Just register the return logic
            this.RegisterReturnLogic(this.RetrunPreLogic);
        }

        private List<string> levelNames = new List<string>() { "SkyBattle", "SkyCool", "SkyWorld", "SpaceWar", "ComeOn", "HellFight", "NewBattle", "King" };
        /// <summary>
        /// Test fill level items
        /// </summary>
        private void FillLevelItems()
        {
            int totalItemsCount = trsLevelItemsParent.childCount;
            for (int i = 0; i < totalItemsCount; i++)
            {
                Transform trs = trsLevelItemsParent.GetChild(i);

                if (trs.childCount > 0)
                    continue;
                GameObject item = NGUITools.AddChild(trs.gameObject, levelItem);
                UILevelItem itemScript = item.GetComponent<UILevelItem>();
                itemScript.SetData(this.levelNames[i], UnityEngine.Random.Range(0, 4));
            }
        }

        public override void HideWindow(System.Action onCompleteHide = null)
        {
            IsLock = true;
            QuitAnimation(delegate
            {
                NGUITools.SetActive(this.gameObject, false);
                if (onCompleteHide != null)
                    onCompleteHide();
            });
        }

        public void EnterAnimation(EventDelegate.Callback onComplete)
        {
            if (twAlpha != null)
            {
                twAlpha.PlayForward();
                EventDelegate.Set(twAlpha.onFinished, onComplete);
            }
        }

        public void QuitAnimation(EventDelegate.Callback onComplete)
        {
            if (twAlpha != null)
            {
                twAlpha.PlayReverse();
                EventDelegate.Set(twAlpha.onFinished, onComplete);
            }
        }

        public override void ResetWindow()
        {
            base.ResetWindow();
            ResetAnimation();
        }

        public void ResetAnimation()
        {
            if (twAlpha != null)
                twAlpha.ResetToBeginning();
        }

        private bool RetrunPreLogic()
        {
            if (!realReturnToMainMenu)
            {
                UICenterMasterManager.Instance.ShowMessageBox(
                    "Do you want to leave level window?\n退出关卡界面?",
                    "Yes",
                    delegate
                    {
                        Debuger.Log("Message Box click YES to leave level window.");
                        UICenterMasterManager.Instance.CloseMessageBox();
                        realReturnToMainMenu = true;
                        UICenterMasterManager.Instance.PopNavigationWindow();
                    },
                    "No",
                    delegate
                    {
                        Debuger.Log("Message Box click NO.");
                        UICenterMasterManager.Instance.CloseMessageBox();
                    });
                return true;
            }
            return false;
        }
    }
}

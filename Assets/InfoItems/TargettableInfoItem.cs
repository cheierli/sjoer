﻿using Assets.Resources;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Assets.InfoItems
{
    public class Targettable : BaseInputHandler, IMixedRealityInputHandler
    {
        [SerializeField]
        protected MixedRealityInputAction selectAction = MixedRealityInputAction.None;

        protected override void RegisterHandlers()
        {
            CoreServices.InputSystem?.RegisterHandler<IMixedRealityInputHandler>(this);
        }

        protected override void UnregisterHandlers()
        {
            CoreServices.InputSystem?.UnregisterHandler<IMixedRealityInputHandler>(this);
        }

        public virtual void OnInputDown(InputEventData eventData)
        {
        }

        public virtual void OnInputUp(InputEventData eventData)
        {
        }
    }

    public class TargettableInfoItem : Targettable
    {
        private bool target = false;
        private bool hover = false;
        private TargettableInfoItem link = null;

        public bool IsTarget
        {
            get { return target; }
            set { target = value; }
        }

        public bool IsHover
        {
            get { return hover; }
            set { hover = value; }
        }

        private bool HasLinkedInfoItem()
        {
            return link != null;
        }

        public void SetLink(TargettableInfoItem link)
        {
            this.link = link;
        }

        public void DestroyLink()
        {
            this.link = null;
        }

        public void OnClick()
        {
            target = !target;
            //Debug.Log("target is now " + target);
            if (HasLinkedInfoItem()) link.IsTarget = target;
        }

        public void OnHoverStart()
        {
            //Debug.Log("Hover start");
            hover = true;
            CancelInvoke();

            if (HasLinkedInfoItem())
            {
                link.CancelInvoke();
                link.IsHover = hover;
            }

        }

        public void OnHoverEnd()
        {
            //Debug.Log("Hover end");
            // Define ms how long it takes before an infoitem disappears when looking at it
            Invoke("InnerOnHoverEnd", (float)Config.Instance.conf.DataSettings["OnLookAwayDisappearDelay"]);
        }

        private void InnerOnHoverEnd()
        {
            hover = false;
            if (HasLinkedInfoItem()) link.IsHover = false;
        }

        public void OnSelect()
        {
            target = true;

            if (HasLinkedInfoItem())
            {
                link.CancelInvoke();
                link.IsTarget = true;
            }
        }

        public override void OnInputDown(InputEventData eventData)
        {
            if (eventData.MixedRealityInputAction == selectAction)
            {
                OnClick();
            }
        }
    }
}
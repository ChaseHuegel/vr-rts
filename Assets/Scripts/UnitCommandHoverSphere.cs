using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

[RequireComponent( typeof( Interactable ) )]
public class UnitCommandHoverSphere : MonoBehaviour
{
        VillagerActor villagerActor;
        private Interactable interactable;
		public VillagerHoverMenu menu;
        public RTSUnitType rtsUnitJob;
		//-------------------------------------------------
		void Awake()
		{
            interactable = this.GetComponent<Interactable>();
            villagerActor = GetComponentInParent<VillagerActor>();
		}		

		//-------------------------------------------------
		// Called when a Hand starts hovering over this object
		//-------------------------------------------------
		private void OnHandHoverBegin( Hand hand )
		{
		}

		//-------------------------------------------------
		// Called when a Hand stops hovering over this object
		//-------------------------------------------------
		private void OnHandHoverEnd( Hand hand )
		{
		}


		//-------------------------------------------------
		// Called every Update() while a Hand is hovering over this object
		//-------------------------------------------------
		private void HandHoverUpdate( Hand hand )
		{
            GrabTypes startingGrabType = hand.GetGrabStarting();
            bool isGrabEnding = hand.IsGrabEnding(this.gameObject);

            if (interactable.attachedToHand == null && startingGrabType != GrabTypes.None)
            {
              	hand.TriggerHapticPulse(1000);
                villagerActor.SetUnitType(rtsUnitJob);

                // // Call this to continue receiving HandHoverUpdate messages,
                // // and prevent the hand from hovering over anything else
                // hand.HoverLock(interactable);
            }
            else if (isGrabEnding)
            {
                // // Call this to undo HoverLock
                // hand.HoverUnlock(interactable);
            }
		}


		//-------------------------------------------------
		// Called when this GameObject becomes attached to the hand
		//-------------------------------------------------
		private void OnAttachedToHand( Hand hand )
        {
		}



		//-------------------------------------------------
		// Called when this GameObject is detached from the hand
		//-------------------------------------------------
		private void OnDetachedFromHand( Hand hand )
		{
		}


		//-------------------------------------------------
		// Called every Update() while this GameObject is attached to the hand
		//-------------------------------------------------
		private void HandAttachedUpdate( Hand hand )
		{
		}

		//-------------------------------------------------
		// Called when this attached GameObject becomes the primary attached object
		//-------------------------------------------------
		private void OnHandFocusAcquired( Hand hand )
		{
		}


		//-------------------------------------------------
		// Called when another attached GameObject becomes the primary attached object
		//-------------------------------------------------
		private void OnHandFocusLost( Hand hand )
		{
		}
}

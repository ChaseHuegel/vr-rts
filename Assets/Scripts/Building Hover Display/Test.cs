using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class Test : MonoBehaviour
{
   

		//-------------------------------------------------
		private void OnHandHoverBegin( Hand hand )
		{
			// if ( holdingHands.IndexOf( hand ) == -1 )
			// {
			// 	if ( hand.isActive )
			// 	{
			// 		hand.TriggerHapticPulse( 800 );
			// 	}
			// }
		}


		//-------------------------------------------------
		private void OnHandHoverEnd( Hand hand )
		{
			// if ( holdingHands.IndexOf( hand ) == -1 )
			// {
			// 	if (hand.isActive)
			// 	{
			// 		hand.TriggerHapticPulse( 500 );
			// 	}
			// }
		}


		//-------------------------------------------------
		private void HandHoverUpdate( Hand hand )
		{
            GrabTypes startingGrabType = hand.GetGrabStarting();

            if (hand.IsGrabbingWithType(GrabTypes.Pinch) && hand.IsGrabbingWithType(GrabTypes.Grip))
            {
                Debug.Log("both");
            }    
//AudioSource.PlayClipAtPoint(GameMaster.GetAudio("knock").GetClip(), transform.position);
            if (startingGrabType != GrabTypes.None)
			{
            //Debug.Log(startingGrabType);
				// PhysicsAttach( hand, startingGrabType );
			}
		}

}

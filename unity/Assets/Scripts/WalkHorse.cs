
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]

public class WalkHorse : MonoBehaviour {

	void OnAnimatorMove()
	{
		Animator animator = GetComponent<Animator>(); 

		if (animator.GetCurrentAnimatorStateInfo(0).shortNameHash != Animator.StringToHash("Horse_Idle") )
		{
			Vector3 newPosition = transform.position;
            var info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.IsTag("Horse_Rev"))
            {
                newPosition.x -= animator.GetFloat("Speed") * Time.deltaTime;
            }
            else
            {
                newPosition.x += animator.GetFloat("Speed") * Time.deltaTime;
            }
			transform.position = newPosition;
		}
	}
}

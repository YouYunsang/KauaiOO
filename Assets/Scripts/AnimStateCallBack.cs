using UnityEngine;

public class AnimStateCallback : StateMachineBehaviour
{
    [Header("Message to send (optional)")]
    public string onEnterMessage;
    public string onExitMessage;

    // Animator가 붙은 GameObject에게 메시지 전송
    // (해당 오브젝트에 같은 이름의 함수가 있으면 호출됨)
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!string.IsNullOrEmpty(onEnterMessage))
            animator.gameObject.SendMessage(onEnterMessage, SendMessageOptions.DontRequireReceiver);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (!string.IsNullOrEmpty(onExitMessage))
            animator.gameObject.SendMessage(onExitMessage, SendMessageOptions.DontRequireReceiver);
    }
}

using UnityEngine;

[ExecuteInEditMode] 
public class EnterScript : MonoBehaviour
{
    private Animator anim;
    private static readonly int Reenter = Animator.StringToHash("Reenter");

    public void Start()
    {
        anim = GetComponent<Animator>();
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            anim.SetTrigger(Reenter);
        }
    }
}

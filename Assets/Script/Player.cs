using UnityEngine;

public class Player : MonoBehaviour
{
    public KeyCode key_PressedDown;
    public KeyCode key_Pressing;
    public KeyCode key_PressedUp;

    public bool isPressDown, isPressing, isPressedUp;
   
    
    // Update is called once per frame
    void Update()
    {
        isPressDown = Input.GetKeyDown(key_PressedDown);
        isPressing = Input.GetKeyDown(key_Pressing);
        isPressedUp = Input.GetKeyDown(key_PressedUp);

        if (isPressDown)
        {
            print("U Pressed down: " + key_PressedDown);

        }
        if (isPressing)
        {
            print("U Pressed down: " + key_Pressing);

        }
        if (isPressedUp)
        {
            print("U Pressed down: " + key_PressedUp);

        }


    }
}

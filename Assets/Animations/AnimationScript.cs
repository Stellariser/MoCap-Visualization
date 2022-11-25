using UnityEngine;

public class AnimationScript : MonoBehaviour
{
    public int maxLoops = 2;
    private int currentLoops = 0;
    public GameObject loopingCanvas;
    public GameObject simulationManager;

    public void counter()
    {
        currentLoops++;
        if (maxLoops <= currentLoops)
        {
            gameObject.GetComponent<Animator>().enabled = false;
            simulationManager.GetComponent<Simulator>().StopAnimation();
        }
        else
        {
            loopingCanvas.SetActive(true);
            Invoke("disableLoopCanvas", 2);
        }
    }

    public void disableLoopCanvas()
    {
        loopingCanvas.SetActive(false);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlurController : MonoBehaviour
{
    [SerializeField] private GameObject camera;
    [SerializeField] private GameObject plane;

    private Vector3 _cameraPosition;
    private bool _blurIsActive = false;
    public Transform marker;
    public DataManager dataManager;
    // Start is called before the first frame update
    // void Start()
    // {
    //     StartCoroutine(Blur());
    // }

    public void RecalibrateBlurPosition()
    {
        _cameraPosition = camera.transform.position;
        _blurIsActive = true;
        marker.position = new Vector3(Camera.main.transform.position.x -0.4f - 0.0275f, Camera.main.transform.position.y - 0.5f - 0.89695f, Camera.main.transform.position.z + 0.3254f);
        dataManager.recalibrate();
    }

    public void DisableBlur()
    {
        _blurIsActive = false;
    }

    // IEnumerator Blur()
    // {
    //     yield return new WaitForSeconds(3);
    //     _cameraPosition = camera.transform.position;
    //     _blurIsActive = true;
    // }

    void Update()
    {
        if (_blurIsActive)
        {
            // check if camera position is within 0.3f of the original position
            if (Vector3.Distance(camera.transform.position, _cameraPosition) > 0.1f)
            {
                //Enable the plane
                plane.SetActive(true);
            }
            else
            {
                //Disable the plane
                plane.SetActive(false);
            }
        }
    }
}

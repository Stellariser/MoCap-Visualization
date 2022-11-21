using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlurController : MonoBehaviour
{
    [SerializeField] private GameObject camera;

    private Vector3 _cameraPosition;
    private bool _isActive = false;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(Blur());
    }

    IEnumerator Blur()
    {
        yield return new WaitForSeconds(3);
        _cameraPosition = camera.transform.position;
        _isActive = true;
        Debug.Log("Coroutine done");
    }

    void Update() {
        if (_isActive) {
            // check if camera position is within 0.3f of the original position
            if (Vector3.Distance(camera.transform.position, _cameraPosition) > 0.2f) {
                // if not, then blur
                Debug.Log("Blur");
                // find the motion blur component of the camera and activate it
                camera.GetComponent<MobileBlur>().enabled = true;
            } else {
                // if so, then unblur
                Debug.Log("Unblur");
                camera.GetComponent<MobileBlur>().enabled = false;
            }
        }
    }
}

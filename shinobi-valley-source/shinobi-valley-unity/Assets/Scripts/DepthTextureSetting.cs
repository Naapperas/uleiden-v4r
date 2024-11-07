using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class DepthTextureSetting : MonoBehaviour
{

    Camera cam;

    private void Awake() {
        cam = this.GetComponent<Camera>();
        cam.depthTextureMode = UnityEngine.DepthTextureMode.Depth;
    }

}

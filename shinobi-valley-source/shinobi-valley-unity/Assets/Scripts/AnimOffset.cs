using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimOffset : MonoBehaviour
{

    public bool pingPong;
    public float pingPongSpeed = 1f;

    public int materialIndex = 0;
    public Vector2 uvAnimationRate = new Vector2(1.0f, 1.0f);
    public string textureName = "_MainTex";

    Vector2 uvOffset = Vector2.zero;
    Renderer rend;
    float randOffset;

    private void Start()
    {
        rend = GetComponent<Renderer>();
        randOffset = Random.Range(0F,10F);
    }

    void LateUpdate()
    {

        if (pingPong)
        {
            uvOffset = new Vector2( Mathf.Sin( Time.time * pingPongSpeed + randOffset) * uvAnimationRate.x, Mathf.Sin(Time.time * pingPongSpeed + randOffset)*uvAnimationRate.y);
        }
        else
        {
            uvOffset += (uvAnimationRate * Time.deltaTime);
        }

        if (rend.enabled)
        {
            rend.materials[materialIndex].SetTextureOffset(textureName, uvOffset);
        }
    }


}

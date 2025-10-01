using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CollisionController : MonoBehaviour
{

    public WebSocketsController WebSocketsController;
    public GameObject plane;
    public RawImage rawImage;

    private Vector3 planeSize;
    private float planeWidth, planeHeight;
    void Start()
    {
        

    }


    private void OnCollisionEnter(Collision collision)
    {
        if(collision.collider.tag == "Ball")
        {
            Vector3 impactPoint = collision.contacts[0].point;
            planeSize = plane.GetComponent<MeshRenderer>().bounds.size;
            planeWidth = planeSize.x;
            planeHeight = planeSize.y;
            Vector3 localPoint = plane.transform.InverseTransformPoint(impactPoint);

            float normalizedX = (localPoint.x + planeWidth/2)/planeWidth;
            float normalizedY = (localPoint.y + planeHeight/2)/planeHeight;

            Debug.Log("Ball hit at : " + impactPoint);

            ImpactPayload impactPayload = new ImpactPayload()
            {
                xDimension = rawImage.texture.width,
                yDimension = rawImage.texture.height,
                colour = null,
                xPosition = Mathf.RoundToInt(normalizedX * rawImage.texture.width),
                yPosition = Mathf.RoundToInt(normalizedY * rawImage.texture.height),
                instruction = "send2dClick"
            };

            WebSocketsController.sendImpactPoint(impactPayload);

        }
    }

    void Update()
    {
        
    }
}

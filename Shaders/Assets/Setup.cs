using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Setup : MonoBehaviour
{
    [SerializeField] Material mat;
    [SerializeField] Transform lightPos;
    [SerializeField] Color ambientLightIntensity;
    [SerializeField] Color lightIntensity;
    void Awake()
    {
        mat = GetComponent<MeshRenderer>().material;
        mat.SetColor("_AmbientLightIntensity", ambientLightIntensity);
        mat.SetColor("_LightIntensity", lightIntensity);
    }

    // Update is called once per frame
    void Update()
    {
        mat.SetVector("_LightPosition", lightPos.position);
        mat.SetColor("_AmbientLightIntensity", ambientLightIntensity);
        mat.SetColor("_LightIntensity", lightIntensity);
    }
}

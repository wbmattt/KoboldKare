using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenericFluidDisplay : MonoBehaviour {
    public GenericReagentContainer container;
    public Renderer targetRenderer;
    public Transform targetTransform;
    public Vector3 scaleDirection = Vector3.up;
    public void Start() {
        scaleDirection = new Vector3(Mathf.Abs(scaleDirection.x), Mathf.Abs(scaleDirection.y), Mathf.Abs(scaleDirection.z));
        container.OnChange.AddListener(OnChanged);
        OnChanged(GenericReagentContainer.InjectType.Vacuum);
    }
    public void OnDestroy() {
        container.OnChange.RemoveListener(OnChanged);
    }
    public void OnChanged(GenericReagentContainer.InjectType injectType) {
        foreach(var m in targetRenderer.materials) {
            m.color = container.GetColor();
        }
        targetTransform.localScale = (Vector3.one - scaleDirection) + (scaleDirection * (container.volume/container.maxVolume));
    }
}

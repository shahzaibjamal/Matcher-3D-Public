using UnityEngine;

public interface IClickable
{
    // We pass the RaycastHit so the object knows WHERE it was hit
    void OnHandleClick(RaycastHit hitInfo);
}
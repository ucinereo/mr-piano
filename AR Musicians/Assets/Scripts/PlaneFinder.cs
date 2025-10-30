using NUnit.Framework;
using UnityEngine;

public abstract class PlaneFinder : MonoBehaviour
{
    #region serialized members
    [Header("Manager")]
    [SerializeField] protected PianoManager manager;
    #endregion

    protected void CapturePoint(Vector3 worldPosition)
    {
        manager.RegisterPoint(worldPosition);
    }
}

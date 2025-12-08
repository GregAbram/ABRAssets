using System;
using System.Collections;
using System.Collections.Generic;
using IVLab.ABREngine;
using UnityEngine;

public class PickHandler : MonoBehaviour
{
    public void onPick(ABRPicker.ABRPick pick)
    {
        IDataImpression idi = ABREngine.Instance.GetDataImpression(pick.guid);
        if (idi == null)
        {
            Debug.Log("No data impression found for pick");
            return;
        }

        if (! pick.abrGameObject.TryGetComponent<EncodedGameObject>(out EncodedGameObject ego))
        {
            Debug.Log("No encoded game object on picked object");
            return;
        }


        SimpleGlyphDataImpression gdi = idi as SimpleGlyphDataImpression;
        if (gdi != null)
        {
            gdi.toggleHilite(ego, pick.instanceId);
        }


        SimpleSurfaceDataImpression sdi = idi as SimpleSurfaceDataImpression;
        if (sdi != null)
        {
            sdi.toggleHilite(); 
        }


        SimpleLineDataImpression ldi = idi as SimpleLineDataImpression;
        if (ldi != null)
        {
            ldi.toggleHilite(pick.instanceId);
        }

        ABREngine.Instance.Render();
    }
}

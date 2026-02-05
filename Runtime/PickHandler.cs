using System;
using System.Collections;
using System.Collections.Generic;
using IVLab.ABREngine;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class PickHandler : ABRPicker.ABRPickHandler
{
    public override void onPick(ABRPicker.ABRPick pick)
    {
        IDataImpression idi = ABREngine.Instance.GetDataImpression(pick.guid);
        if (idi == null)
        {
            Debug.Log("No data impression found for pick");
            return;
        }

        string msg = "";

        if (! pick.abrGameObject.TryGetComponent<EncodedGameObject>(out EncodedGameObject ego))
        {
            Debug.Log("No encoded game object on picked object");
            return;
        }

        SimpleGlyphDataImpression gdi = idi as SimpleGlyphDataImpression;
        if (gdi != null)
        {
            msg = msg + $"Type: Glyph\n";
            msg = msg + $"Which: {pick.id}\n";
            gdi.toggleHilite(ego, pick.id);
        }

        SimpleSurfaceDataImpression sdi = idi as SimpleSurfaceDataImpression;
        if (sdi != null)
        {            
            msg = msg + $"Type: Surface\nWhich: {pick.id}\nbarycentric weights: {pick.barycentric_weights}\n";

            sdi.toggleHilite(); 
            SurfaceDatasetAccessor sda = new SurfaceDatasetAccessor(pick.dataset, idi);

            if (sda.GetScalarValue(pick.id, pick.barycentric_weights, out float v))
                msg = msg + $"Scalar value: {v}\n";
        }

        SimpleLineDataImpression ldi = idi as SimpleLineDataImpression;
        if (ldi != null)
        {
            msg = msg + $"Type: Line\nWhich: {pick.id}\n";
            ldi.toggleHilite(pick.id);
        }

        ABREngine.Instance.Render();

        msg = msg + $"Color variable: {idi.GetColorVariableName()}\n";
        msg = msg + $"Point: {pick.point}\n";

        Debug.Log("Pick:\n" + msg);
    }
}

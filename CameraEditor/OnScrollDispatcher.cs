using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.EventSystems;

namespace CameraEditor
{
    public class OnScrollDispatcher : UIBehaviour, IScrollHandler
    {
        public event Action<PointerEventData> onScroll; 
        public void OnScroll(PointerEventData eventData)
        {
            if (this.onScroll != null)
                this.onScroll(eventData);
        }
    }
}

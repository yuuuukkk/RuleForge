using System;
using UnityEngine;

namespace RuleForge.UI
{
    public static class RuntimePanelCoordinator
    {
        private static UnityEngine.Object activeOwner;
        private static Action closeActive;

        public static bool HasActivePanel => activeOwner != null;

        public static void Open(UnityEngine.Object owner, Action closeAction)
        {
            if (owner == null)
            {
                return;
            }

            if (activeOwner != null && activeOwner != owner)
            {
                Action previousClose = closeActive;
                activeOwner = null;
                closeActive = null;
                previousClose?.Invoke();
            }

            activeOwner = owner;
            closeActive = closeAction;
        }

        public static void Close(UnityEngine.Object owner)
        {
            if (owner == null || activeOwner != owner)
            {
                return;
            }

            activeOwner = null;
            closeActive = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            activeOwner = null;
            closeActive = null;
        }
    }
}

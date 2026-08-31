using System.Collections.Generic;
using UnityEngine;

namespace RuleForge.UI
{
    public static class RuntimeInputGate
    {
        private static readonly HashSet<int> Blockers = new HashSet<int>();
        private static CursorLockMode cursorLockBeforeBlocking;
        private static bool cursorVisibleBeforeBlocking;

        public static bool IsBlocked => Blockers.Count > 0;

        public static void SetBlocked(Object owner, bool blocked)
        {
            if (owner == null)
            {
                return;
            }

            int ownerId = owner.GetInstanceID();
            if (blocked)
            {
                if (Blockers.Count == 0)
                {
                    cursorLockBeforeBlocking = Cursor.lockState;
                    cursorVisibleBeforeBlocking = Cursor.visible;
                }

                Blockers.Add(ownerId);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            if (!Blockers.Remove(ownerId) || Blockers.Count > 0)
            {
                return;
            }

            Cursor.lockState = cursorLockBeforeBlocking;
            Cursor.visible = cursorVisibleBeforeBlocking;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Blockers.Clear();
            cursorLockBeforeBlocking = CursorLockMode.None;
            cursorVisibleBeforeBlocking = true;
        }
    }
}

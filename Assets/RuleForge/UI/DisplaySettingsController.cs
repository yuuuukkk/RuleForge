using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuleForge.UI
{
    internal sealed class DisplaySettingsController
    {
        private Resolution[] availableResolutions =
            Array.Empty<Resolution>();
        private int selectedResolutionIndex;
        private bool selectedFullscreen;

        public bool SelectedFullscreen
        {
            get => selectedFullscreen;
            set => selectedFullscreen = value;
        }

        public string ResolutionLabel
        {
            get
            {
                if (availableResolutions.Length == 0)
                {
                    return Screen.width + " × " + Screen.height;
                }

                Resolution selected =
                    availableResolutions[selectedResolutionIndex];
                return selected.width + " × " + selected.height;
            }
        }

        public void Refresh()
        {
            Resolution[] supported = Screen.resolutions;
            List<Resolution> unique = new List<Resolution>();
            for (int index = 0; index < supported.Length; index++)
            {
                Resolution candidate = supported[index];
                bool duplicate = false;
                for (int existingIndex = 0;
                     existingIndex < unique.Count;
                     existingIndex++)
                {
                    if (unique[existingIndex].width == candidate.width &&
                        unique[existingIndex].height == candidate.height)
                    {
                        duplicate = true;
                        break;
                    }
                }

                if (!duplicate)
                {
                    unique.Add(candidate);
                }
            }

            availableResolutions = unique.ToArray();
            selectedResolutionIndex = 0;
            int closestDistance = int.MaxValue;
            for (int index = 0; index < availableResolutions.Length; index++)
            {
                int distance = Mathf.Abs(
                                   availableResolutions[index].width -
                                   Screen.width) +
                               Mathf.Abs(
                                   availableResolutions[index].height -
                                   Screen.height);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    selectedResolutionIndex = index;
                }
            }

            selectedFullscreen = Screen.fullScreen;
        }

        public void ChangeSelection(int direction)
        {
            if (availableResolutions.Length == 0)
            {
                return;
            }

            selectedResolutionIndex =
                (selectedResolutionIndex + direction +
                 availableResolutions.Length) % availableResolutions.Length;
        }

        public void Apply()
        {
            if (availableResolutions.Length == 0)
            {
                Screen.fullScreen = selectedFullscreen;
                return;
            }

            Resolution selected =
                availableResolutions[selectedResolutionIndex];
            Screen.SetResolution(
                selected.width,
                selected.height,
                selectedFullscreen);
        }

        public bool Draw(
            Rect panelRect,
            GUIStyle resultStyle,
            GUIStyle bodyStyle,
            GUIStyle dangerButtonStyle)
        {
            GUI.Label(
                new Rect(panelRect.x + 44f, panelRect.y + 100f,
                    panelRect.width - 88f, 46f),
                "显示设置",
                resultStyle);

            GUI.Label(
                new Rect(panelRect.x + 80f, panelRect.y + 168f,
                    panelRect.width - 160f, 30f),
                "分辨率",
                bodyStyle);
            if (GUI.Button(
                    new Rect(panelRect.x + 80f, panelRect.y + 208f,
                        58f, 46f),
                    "◀",
                    GUI.skin.button))
            {
                ChangeSelection(-1);
            }

            GUI.Label(
                new Rect(panelRect.x + 150f, panelRect.y + 211f,
                    panelRect.width - 300f, 40f),
                ResolutionLabel,
                resultStyle);
            if (GUI.Button(
                    new Rect(panelRect.xMax - 138f,
                        panelRect.y + 208f, 58f, 46f),
                    "▶",
                    GUI.skin.button))
            {
                ChangeSelection(1);
            }

            SelectedFullscreen = GUI.Toggle(
                new Rect(panelRect.x + 80f, panelRect.y + 278f,
                    panelRect.width - 160f, 36f),
                SelectedFullscreen,
                "全屏模式");
            if (GUI.Button(
                    new Rect(panelRect.x + 72f, panelRect.y + 332f,
                        panelRect.width - 144f, 52f),
                    "应用设置",
                    GUI.skin.button))
            {
                Apply();
            }

            return GUI.Button(
                new Rect(panelRect.x + 72f, panelRect.y + 402f,
                    panelRect.width - 144f, 48f),
                "返回",
                dangerButtonStyle);
        }
    }
}

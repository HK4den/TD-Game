using UnityEngine;
using UnityEditor;
using System.Linq;

namespace FolderColor
{
    public class CustomWindowFileImage : EditorWindow
    {
        string assetPath;
        private Vector2 scrollPosition;

        private const float ButtonSize = 100f;
        private const float ButtonPadding = 10f;
        private const float HeaderHeight = 110f;

        public static void ShowWindow(string assetPathGive)
        {
            CustomWindowFileImage window = GetWindow<CustomWindowFileImage>("Custom Folder");
            window.assetPath = assetPathGive;
            window.Show();
        }

        private void OnGUI()
        {
            if (GUI.Button(new Rect(ButtonPadding, ButtonPadding, ButtonSize, ButtonSize), "None"))
            {
                if (ProjectAssetViewerCustomisation.modificationData.assetModified.Contains(assetPath))
                {
                    RemoveReference(assetPath);
                    ProjectAssetViewerCustomisation.SaveData();
                }

                Close();
            }

			string path = ProjectAssetViewerCustomisation.FindScriptPathByName("CustomWindowFileImage");
			path = path.Replace("/Editor/CustomWindowFileImage.cs", "");

			string[] texturesPath = AssetDatabase.FindAssets("t:texture2D", new[] { path });

            float scrollViewHeight = Mathf.Max(0f, position.height - HeaderHeight);
            float contentWidth = Mathf.Max(ButtonSize + ButtonPadding * 2f, position.width - 18f);
            int buttonsPerRow = Mathf.Max(1, Mathf.FloorToInt((contentWidth - ButtonPadding) / (ButtonSize + ButtonPadding)));
            int rowCount = Mathf.CeilToInt((float)texturesPath.Length / buttonsPerRow);
            float contentHeight = Mathf.Max(scrollViewHeight, rowCount * (ButtonSize + ButtonPadding) + ButtonPadding);

            Rect scrollViewRect = new Rect(0f, HeaderHeight, position.width, scrollViewHeight);
            Rect contentRect = new Rect(0f, 0f, contentWidth, contentHeight);
            scrollPosition = GUI.BeginScrollView(scrollViewRect, scrollPosition, contentRect);

            for (int i = 0; i < texturesPath.Length; i++)
            {
                Texture2D texture = (Texture2D)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(texturesPath[i]), typeof(Texture2D));

                float x = (i % buttonsPerRow) * (ButtonSize + ButtonPadding) + ButtonPadding;
                float y = Mathf.Floor(i / buttonsPerRow) * (ButtonSize + ButtonPadding) + ButtonPadding;

                if (GUI.Button(new Rect(x, y, ButtonSize, ButtonSize), texture))
                {
                    if (ProjectAssetViewerCustomisation.modificationData.assetModified.Contains(assetPath)) RemoveReference(assetPath);

                    ProjectAssetViewerCustomisation.modificationData.assetModified.Add(assetPath);
                    ProjectAssetViewerCustomisation.modificationData.assetModifiedTexturePath.Add(AssetDatabase.GUIDToAssetPath(texturesPath[i]));
                    ProjectAssetViewerCustomisation.SaveData();

                    Close();
                }
            }

            GUI.EndScrollView();
        }

        private static void RemoveReference(string assetPath)
        {
            int i = ProjectAssetViewerCustomisation.modificationData.assetModified.IndexOf(assetPath);
            ProjectAssetViewerCustomisation.modificationData.assetModified.RemoveAt(i);
            ProjectAssetViewerCustomisation.modificationData.assetModifiedTexturePath.RemoveAt(i);
        }
    }
}

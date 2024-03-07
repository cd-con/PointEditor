using PointEditor.Layouts;
using PointEditor.Utility.Actions;
using PointEditor.Utility.TreeViewStorage;
using System.Collections.Generic;

namespace PointEditor.Utility.Scenes
{
    public class Scene
    {
        public List<IAction> l_Actions = new();
        public List<TreeViewGeneric> l_TreeItems = new();

        private EditorLayout canvasCtx;

        public Scene()
        {
            canvasCtx = new();
        }

        public EditorLayout Get() => canvasCtx;
    }
}

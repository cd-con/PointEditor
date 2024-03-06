using System;

namespace PointEditor.Utility.Actions.Objects.Generic
{
    // TODO
    internal class SelectObject : IAction
    {
        string selected;
        public void Do(object[] args)
        {
            // Args
            // 0 - selected object
            selected = (string)args[0];
        }

        public void Undo()
        {
            throw new NotImplementedException();
        }
    }
}

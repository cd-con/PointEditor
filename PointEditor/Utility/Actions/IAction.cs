﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PointEditor.Utility.Actions;

internal interface IAction
{
    public object Undo();
    public object Do(object[] args);
}
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
$if$ ($targetframeworkversion$ == 3.5)using System.Linq;
$endif$using System.Text;
using Microsoft.Deployment.WindowsInstaller;

namespace $safeprojectname$
{
    public class CustomActions
    {
        [CustomAction]
        public static ActionResult CustomAction1(Session session)
        {
            session.Log("Begin CustomAction1");

            return ActionResult.Success;
        }
    }
}

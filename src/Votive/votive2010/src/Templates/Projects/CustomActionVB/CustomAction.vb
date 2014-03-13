Public Class CustomActions

    <CustomAction()> _
    Public Shared Function CustomAction1(ByVal session As Session) As ActionResult
        session.Log("Begin CustomAction1")

        Return ActionResult.Success
    End Function

End Class

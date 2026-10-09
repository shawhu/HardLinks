public class ClickThroughLabel : Label
{
    private const int WM_NCHITTEST = 0x0084;
    private const int HTTRANSPARENT = -1;

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_NCHITTEST)
        {
            m.Result = (nint)HTTRANSPARENT;
            return;
        }

        base.WndProc(ref m);
    }
}

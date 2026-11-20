using RimWorld;

namespace tsoa.core.anomaly;

public class ITab_VoidGateContents : ITab_ContentsTransporter
{
    public ITab_VoidGateContents()
    {
        // have to cancel to unload, can't remove individual items
        canRemoveThings = false;
    }
}

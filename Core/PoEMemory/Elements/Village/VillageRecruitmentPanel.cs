using System.Collections.Generic;

namespace ExileCore.PoEMemory.Elements.Village;
public class VillageRecruitmentPanel : Element
{
    public List<VillageRecruitmentPanelWorkerElement> OfferedWorkers
    {
        get
        {
            throw new global::System.NotImplementedException("Body protected in source DLL; not recoverable.");
        }
    }

    public List<VillageRecruitmentPanelWorkerElement> CurrentWorkers => (List<VillageRecruitmentPanelWorkerElement>)(object)new int[3]
    {
        0,
        5,
        1
    };
}
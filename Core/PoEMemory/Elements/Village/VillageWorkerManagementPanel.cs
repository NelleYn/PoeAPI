using System.Collections.Generic;

namespace ExileCore.PoEMemory.Elements.Village;
public class VillageWorkerManagementPanel : Element
{
    public List<VillageWorkerManagementPanelWorkerElement> AvailableWorkers => (List<VillageWorkerManagementPanelWorkerElement>)(object)new int[4];
    public List<VillageWorkerManagementPanelWorkerElement> AssignedWorkers => (List<VillageWorkerManagementPanelWorkerElement>)(object)new int[4]
    {
        0,
        0,
        2,
        1
    };
}
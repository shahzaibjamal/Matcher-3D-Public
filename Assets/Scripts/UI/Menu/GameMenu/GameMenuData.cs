public class GameMenuData : MenuData
{
    public SlotManager SlotManager { get; private set; }
    public int SlotCount { get; private set; }

    public GameMenuData(SlotManager logic, int count)
    {
        SlotManager = logic;
        SlotCount = count;
    }
}
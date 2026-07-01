public class SlotReference
{
    public SlotType SlotType { get; private set; }
    public int SlotIndex { get; private set; }

    public SlotReference(SlotType slotType, int slotIndex)
    {
        SlotType = slotType;
        SlotIndex = slotIndex;
    }
}
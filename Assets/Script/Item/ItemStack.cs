[System.Serializable]
public class ItemStack
{
    public ItemData item;
    public int amount;

    public bool IsEmpty => item == null || amount <= 0;

    public ItemStack(ItemData item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }

    public void Clear()
    {
        item = null;
        amount = 0;
    }
}

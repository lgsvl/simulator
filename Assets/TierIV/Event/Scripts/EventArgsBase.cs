
public class EventArgsBase
{
    private int typeHash;

    public int TypeHash
    {
        get { return typeHash; }
    }

    public EventArgsBase()
    {
        typeHash = this.GetType().GetHashCode();
    }
}

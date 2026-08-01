namespace Marmot;

public partial class World {

    private int _nextEntityId;
    public int CreateEntity() => _nextEntityId++;
}
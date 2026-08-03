namespace Marmot;

public abstract class Runner {

    public virtual void Init() {}
    public virtual void Loop() {}
    public virtual void FixedLoop() {}
    public virtual void Exit() {}
}
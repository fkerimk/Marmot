namespace Marmot;

public abstract class Scene {

    internal static Scene? Current;

    private readonly Dictionary<Type, object> _components = [];

    private Dictionary<int, T> GetSceneComponents<T>() where T : struct {

        if (!_components.TryGetValue(typeof(T), out var store))
            _components[typeof(T)] = store = new Dictionary<int, T>();

        return (Dictionary<int, T>)store;
    }

    public static Dictionary<int, T> GetComponents<T>() where T : struct
        => Current?.GetSceneComponents<T>() ?? throw new NullReferenceException("Current scene is null");

    public static void SetCurrent(Scene scene) {

        Current?.Exit();
        Current = scene;
        scene.Init();
    }

    private int _nextEntityId;
    public int CreateEntity() => _nextEntityId++;

    public virtual void Init() {}
    public virtual void Loop() {}
    public virtual void Exit() {}
}
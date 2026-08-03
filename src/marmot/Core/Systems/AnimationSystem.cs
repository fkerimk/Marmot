using Marmot.Backend.Rendering;

namespace Marmot;

public static unsafe class AnimationSystem {

    public static void Update() {

        foreach (var (id, animation) in Scene.GetComponents<AnimationComponent>()) {

            var model = id.RequireModel();
            var anim = id.GetAnimation();

            anim.Timer += Time.Delta;

            while (anim.Timer >= model.Value.FrameDuration) {

                anim.Timer -= model.Value.FrameDuration;
                anim.Frame = (anim.Frame + 1) % model.Value.RlAnims[anim.Animation].KeyFrameCount;
            }

            Rl.SetAnimationFrame(model.Value.RlModel, model.Value.RlAnims[anim.Animation], anim.Frame);

            id.SetAnimation(anim);
        }
    }
}
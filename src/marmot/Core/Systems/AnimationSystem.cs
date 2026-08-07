using Marmot.Backend.Rendering;

namespace Marmot;

public static unsafe class AnimationSystem {

    public static void Update() {

        foreach (var (id, animator) in Scene.GetComponents<Animator>()) {

            var model = id.RequireModel();
            var anim = animator;

            anim.Timer += Time.Delta;

            while (anim.Timer >= model.FrameDuration) {

                anim.Timer -= model.FrameDuration;
                anim.Frame = (anim.Frame + 1) % model.Resource.RlAnims[anim.Animation].KeyFrameCount;
            }

            Rl.SetAnimationFrame(model.Resource.RlModel, model.Resource.RlAnims[anim.Animation], anim.Frame);

            id.SetAnimator(anim);
        }
    }
}
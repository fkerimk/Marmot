using Marmot.Backend.Rendering;

namespace Marmot;

public static unsafe class AnimationSystem {

    public static void Update(World world) {

        foreach (var (id, animation) in world.AnimationComponents) {

            var model = world.RequireModel(id);
            var anim = world.GetAnimation(id);

            anim.Timer += Time.Delta;

            while (anim.Timer >= model.Value.FrameDuration) {

                anim.Timer -= model.Value.FrameDuration;
                anim.Frame = (anim.Frame + 1) % model.Value.RlAnims[anim.Animation].KeyFrameCount;
            }

            Rl.SetAnimationFrame(model.Value.GetRlModel(), model.Value.RlAnims[anim.Animation], anim.Frame);

            world.SetAnimation(id, anim);
        }
    }
}
using FargowiltasSouls.Core;
using FargowiltasSouls.Core.Systems;
using Luminance.Core.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace FargowiltasSouls.Common.Graphics.Metaballs
{
    public class PlanteraMetaball : MetaballType
    {
        public override string MetaballAtlasTextureToUse => "FargowiltasSouls.MetaballBase";

        public override bool ShouldRender => ActiveParticleCount >= 1;

        public override Color EdgeColor => SoulConfig.Instance.BossRecolors && WorldSavingSystem.EternityMode ? Color.DeepSkyBlue : Color.LimeGreen;

        public override Func<Texture2D>[] LayerTextures =>
        [
        () => SoulConfig.Instance.BossRecolors && WorldSavingSystem.EternityMode ? ModContent.Request<Texture2D>("FargowiltasSouls/Assets/Textures/Metaballs/LightBluePixel").Value : ModContent.Request<Texture2D>("FargowiltasSouls/Assets/Textures/Metaballs/LimeGreenPixel").Value
        ];

        public override bool LayerIsFixedToScreen(int layerIndex) => true;

        public override bool ShouldKillParticle(MetaballInstance particle) => particle.Size <= 4f;
        public override bool DrawnManually => true;
        public override void Load()
        {
            On_Main.DrawProjectiles += DrawMetaballs;
            base.Load();
        }

        private static void DrawMetaballs(On_Main.orig_DrawProjectiles orig, Main self)
        {
            PlanteraMetaball metaball = ModContent.GetInstance<PlanteraMetaball>();
            if (metaball.ShouldRender)
            {
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                for (int i = 0; i < metaball.LayerTargets.Count; i++)
                {
                    metaball.PrepareShaderForTarget(i);
                    Main.spriteBatch.Draw((RenderTarget2D)metaball.LayerTargets[i], Main.screenLastPosition - Main.screenPosition, Color.White * 0.8f);
                }
                Main.spriteBatch.End();
            }

            orig(self);
        }
        public override void UpdateParticle(MetaballInstance particle)
        {
            particle.Velocity *= 0.99f;
            particle.Size *= 0.955f;
        }
    }

    public class PlanteraMetaballPink : MetaballType
    {
        public override string MetaballAtlasTextureToUse => "FargowiltasSouls.MetaballBase";

        public override bool ShouldRender => ActiveParticleCount >= 1;

        public override Color EdgeColor => Color.HotPink;

        public override Func<Texture2D>[] LayerTextures => new Func<Texture2D>[]
        {
        () => ModContent.Request<Texture2D>("FargowiltasSouls/Assets/Textures/Metaballs/DarkPinkPixel").Value
        };

        public override bool LayerIsFixedToScreen(int layerIndex) => true;

        public override bool ShouldKillParticle(MetaballInstance particle) => particle.Size <= 4f;
        public override bool DrawnManually => true;
        public override void Load()
        {
            On_Main.DrawProjectiles += DrawMetaballs;
            base.Load();
        }

        private static void DrawMetaballs(On_Main.orig_DrawProjectiles orig, Main self)
        {
            var metaball = ModContent.GetInstance<PlanteraMetaballPink>();
            if (metaball.ShouldRender)
            {
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                for (int i = 0; i < metaball.LayerTargets.Count; i++)
                {
                    metaball.PrepareShaderForTarget(i);
                    Main.spriteBatch.Draw((RenderTarget2D)metaball.LayerTargets[i], Main.screenLastPosition - Main.screenPosition, Color.White * 0.8f);
                }
                Main.spriteBatch.End();
            }

            orig(self);
        }
        public override void UpdateParticle(MetaballInstance particle)
        {
            particle.Velocity *= 0.99f;
            particle.Size *= 0.955f;
        }
    }
}

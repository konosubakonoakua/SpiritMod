﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SpiritMod.Buffs.Zones;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace SpiritMod.Projectiles.Summon.Zones
{
	class StaminaZone : ModProjectile, IDrawAdditive
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Stamina Zone");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 4;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }

		public override void SetDefaults()
		{
			projectile.friendly = true;
			projectile.hostile = false;
            projectile.tileCollide = false;
			projectile.penetrate = 4;
            projectile.timeLeft = Projectile.SentryLifeTime;
            projectile.height = 110;
            projectile.sentry = true;
            projectile.width = 110;
            projectile.scale = 1.2f;
        }

		public override void AI()
		{
			Player player = Main.LocalPlayer;

			if (!Main.player[projectile.owner].HasBuff(ModContent.BuffType<StaminaZoneTimer>()))
				projectile.Kill();

			if (player.DistanceSQ(projectile.Center) < 100 * 100)
                player.AddBuff(ModContent.BuffType<StaminaZoneBuff>(), 300);
		}

		public void AdditiveCall(SpriteBatch spriteBatch) => ZoneHelper.ZoneAdditiveDraw(spriteBatch, projectile, new Color(44, 168, 67), "SpiritMod/Projectiles/Summon/Zones/StaminaZone");
		public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor) => ZoneHelper.ZonePreDraw(projectile, mod.GetTexture("Projectiles/Summon/Zones/StaminaZone_Glow"));

		public override void Kill(int timeLeft)
        {
            for (int k = 0; k < 30; k++)
            {
                Dust d = Dust.NewDustPerfect(projectile.Center, 131, Vector2.One.RotatedByRandom(6.28f) * Main.rand.NextFloat(6), 0, default, 0.5f);
                d.noGravity = true;
            }
        }
    }
}
